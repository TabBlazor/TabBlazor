#nullable enable
using Microsoft.Extensions.Logging;

namespace TabBlazor;

/// <summary>
/// DataView's engine: owns paging, sort, search and filter state, builds <see cref="DataRequest"/>s and runs the
/// request lifecycle (search delay, cancel-and-wait, stale-response guard, loading and error state). It knows
/// nothing about rendering; views read its state and call its methods.
/// </summary>
internal sealed class DataViewEngine<TItem> : IDisposable
{
    private readonly Func<Func<Task>, Task> dispatch;
    private readonly Func<Task> render;
    private readonly TimeProvider timeProvider;
    private readonly ILogger logger;

    // Cursor of each visited page: cursors[i] is the "after" cursor for page i (null for the first page).
    private readonly List<string?> cursors = [null];

    // One call to the source at a time: loads and extra fetches (virtual scrolling) take turns.
    private readonly SemaphoreSlim sourceGate = new(1, 1);

    private CancellationTokenSource? loadCts;
    private CancellationTokenSource fetchCts = new();
    private Task? loadTask;
    private int loadVersion;
    private CancellationTokenSource? searchDelayCts;
    private IReadOnlyList<DataColumnInfo> requestColumns = [];

    // The last page index known to exist when the source reports no total (found by paging past the end).
    private int? lastKnownPageIndex;

    public DataViewEngine(Func<Func<Task>, Task> dispatch, Func<Task> render, TimeProvider timeProvider, ILogger logger)
    {
        this.dispatch = dispatch;
        this.render = render;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    // Configuration

    public IDataViewSource<TItem>? Source { get; set; }
    public IReadOnlyList<IDataViewColumn<TItem>> Columns { get; private set; } = [];
    public DataColumnInfo? KeyInfo { get; private set; }
    public Func<TItem, object?>? KeyGetter { get; private set; }

    /// <summary>When true, the key is sent as the sort when nothing else is sorted (for stable offset paging).</summary>
    public bool SortByKeyWhenUnsorted { get; set; } = true;

    public SearchMode SearchMode { get; set; } = SearchMode.Words;
    public int MaxSearchTerms { get; set; } = SearchSpec.DefaultMaxTerms;
    public int SearchDelay { get; set; } = 300;

    // State

    public int PageIndex { get; private set; }
    public int PageSize { get; private set; } = 25;
    public SortSpec? Sort { get; private set; }
    public string? SearchText { get; private set; }
    public string SearchInput { get; private set; } = string.Empty;
    public IReadOnlyList<FilterSpec> Filters { get; private set; } = [];

    // Results

    public IReadOnlyList<TItem> Items { get; private set; } = [];
    public int? TotalCount { get; private set; }
    public string? NextCursor { get; private set; }
    public bool HasLoaded { get; private set; }
    public bool IsLoading { get; private set; }
    public Exception? Error { get; private set; }
    public DataRequest? LastRequest { get; private set; }

    /// <summary>Bumped when rows must re-render even though their item did not change.</summary>
    public int RenderVersion { get; private set; }

    /// <summary>Raised after a page has loaded.</summary>
    public Func<DataPage<TItem>, Task>? PageLoaded { get; set; }

    /// <summary>Raised when the user changes paging, sort, search or filters.</summary>
    public Func<Task>? StateChanged { get; set; }

    /// <summary>Asked before the user changes paging, sort, search or filters; false keeps the current state (for example to keep an unsaved edit).</summary>
    public Func<Task<bool>>? BeforeUserStateChange { get; set; }

    private async Task<bool> CanChangeStateAsync() => BeforeUserStateChange == null || await BeforeUserStateChange();

    /// <summary>Replaces a loaded row (after it was saved) without reloading.</summary>
    public void ReplaceItem(Func<TItem, bool> match, TItem item)
    {
        Items = Items.Select(i => match(i) ? item : i).ToList();
        RenderVersion++;
    }

    public PagingMode PagingMode => Source?.PagingMode ?? PagingMode.Offset;

    public bool HasSearchOrFilters => !string.IsNullOrWhiteSpace(SearchText) || Filters.Count > 0;

    public bool HasPreviousPage => PageIndex > 0;

    public bool HasNextPage => PagingMode == PagingMode.Cursor
        ? NextCursor != null
        : TotalCount is { } total ? (PageIndex + 1) * PageSize < total
        : Items.Count >= PageSize && (lastKnownPageIndex == null || PageIndex < lastKnownPageIndex);

    /// <summary>The number of pages, when the total is known.</summary>
    public int? PageCount => TotalCount is { } total ? Math.Max(1, (int)Math.Ceiling(total / (double)Math.Max(1, PageSize))) : null;

    public void InvalidateRows() => RenderVersion++;

    public void SetColumns(IReadOnlyList<IDataViewColumn<TItem>> columns)
    {
        var infos = columns.Select((c, i) => c.Resolve(i)).ToList();
        var duplicate = infos.GroupBy(i => i.Id, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
        {
            throw new InvalidOperationException(
                $"Two DataView columns have the Id '{duplicate.Key}'. Set Id on one of them to tell them apart.");
        }

        Columns = columns;
        BuildRequestColumns();
        Filters = SanitizeFilters(Filters);

        if (Sort != null && !columns.Any(c => c.Sortable && c.Id == Sort.Column) && Sort.Column != KeyInfo?.Id)
        {
            Sort = null;
        }
    }

    public void SetKey(Expression<Func<TItem, object>>? keySelector)
    {
        if (keySelector == null)
        {
            KeyGetter = null;
            KeyInfo = null;
            BuildRequestColumns();
            return;
        }

        var compiled = keySelector.Compile();
        KeyGetter = item => item == null ? null : compiled(item);
        var body = DataColumnInfoBuilder.StripConvert(keySelector.Body);
        var path = DataColumnInfoBuilder.GetMemberPath(keySelector);
        if (path == null)
        {
            logger.LogWarning("DataView KeySelector {KeySelector} is not a member path; the key is not added as a sort.", keySelector);
            KeyInfo = null;
        }
        else
        {
            KeyInfo = DataColumnInfoBuilder.Create(body.Type, Expression.Lambda(body, keySelector.Parameters));
        }

        BuildRequestColumns();
    }

    private static DataColumnInfo RequestInfo(IDataViewColumn<TItem> column, DataColumnInfo info) =>
        info with { Sortable = column.Sortable, Filterable = column.Filterable, Searchable = column.Searchable };

    /// <summary>True when the column at <paramref name="index"/> still resolves to what the requests use.</summary>
    public bool IsUnchangedColumn(int index, IDataViewColumn<TItem> column) =>
        index < Columns.Count && index < requestColumns.Count &&
        Equals(requestColumns[index], RequestInfo(column, column.Resolve(index)));

    private void BuildRequestColumns()
    {
        var infos = Columns.Select(c => RequestInfo(c, c.Info)).ToList();
        if (KeyInfo != null)
        {
            var existing = infos.FirstOrDefault(i => i.Id == KeyInfo.Id);
            if (existing != null)
            {
                KeyInfo = existing;
            }
            else
            {
                infos.Add(KeyInfo);
            }
        }

        requestColumns = infos;
    }

    public void ApplyDefaultSort()
    {
        var column = Columns.FirstOrDefault(c => c.Sortable && c.DefaultSort != null);
        Sort = column == null ? null : new SortSpec(column.Id, column.DefaultSort == SortOrder.Descending);
    }

    public void SetPageSize(int pageSize)
    {
        PageSize = Math.Max(1, pageSize);
        ResetPaging();
    }

    public void ResetPaging()
    {
        PageIndex = 0;
        lastKnownPageIndex = null;
        cursors.Clear();
        cursors.Add(null);
    }

    /// <summary>Builds the request for the current state, or for another page of it.</summary>
    public DataRequest BuildRequest(PageRequest? pageOverride = null)
    {
        var sorts = new List<SortSpec>();
        if (Sort != null)
        {
            sorts.Add(Sort);
        }

        if (KeyInfo != null && (Sort == null ? SortByKeyWhenUnsorted : Sort.Column != KeyInfo.Id))
        {
            sorts.Add(new SortSpec(KeyInfo.Id, Sort?.Descending ?? false));
        }

        var searchColumns = Columns.Where(c => c.Searchable).Select(c => c.Id).ToList();
        var search = searchColumns.Count == 0 ? null : SearchSpec.Parse(SearchText, SearchMode, searchColumns, MaxSearchTerms);

        var page = PagingMode == PagingMode.Cursor
            ? PageRequest.Cursor(cursors[Math.Min(PageIndex, cursors.Count - 1)], PageSize)
            : PageRequest.Offset(PageIndex * PageSize, PageSize);

        return new DataRequest(pageOverride ?? page, sorts, search, Filters, requestColumns);
    }

    /// <summary>
    /// Loads the current page. A new load cancels the previous one and waits for it to finish before calling the
    /// source, so two loads never overlap (a shared DbContext stays safe) and a late response is ignored.
    /// </summary>
    public Task LoadAsync()
    {
        var previous = loadTask;
        loadCts?.Cancel();

        // Extra fetches belong to the query being replaced.
        fetchCts.Cancel();
        fetchCts = new CancellationTokenSource();

        var cts = new CancellationTokenSource();
        loadCts = cts;
        var version = ++loadVersion;
        var task = RunLoadAsync(previous, cts.Token, version);
        loadTask = task;
        return task;
    }

    private async Task RunLoadAsync(Task? previous, CancellationToken cancellationToken, int version)
    {
        if (previous != null)
        {
            try
            {
                await previous;
            }
            catch
            {
                // The previous load reports its own errors.
            }
        }

        if (cancellationToken.IsCancellationRequested || version != loadVersion)
        {
            return;
        }

        if (Source == null)
        {
            return;
        }

        IsLoading = true;
        await render();

        DataPage<TItem>? page = null;
        Exception? error = null;
        var request = BuildRequest();
        try
        {
            await sourceGate.WaitAsync(cancellationToken);
            try
            {
                page = await Source.GetPageAsync(request, cancellationToken);
            }
            finally
            {
                sourceGate.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            error = ex;
        }

        if (cancellationToken.IsCancellationRequested || version != loadVersion)
        {
            return;
        }

        IsLoading = false;
        LastRequest = request;
        if (error != null)
        {
            logger.LogError(error, "DataView failed to load data.");
            Error = error;
            await render();
            return;
        }

        page ??= DataPage<TItem>.Empty;
        Error = null;
        Items = page.Items ?? [];
        TotalCount = page.TotalCount;
        NextCursor = page.NextCursor;
        HasLoaded = true;
        RenderVersion++;
        if (PagingMode == PagingMode.Cursor)
        {
            while (cursors.Count <= PageIndex + 1)
            {
                cursors.Add(null);
            }

            cursors[PageIndex + 1] = NextCursor;
        }

        // A page past the end (the last row of the last page was removed, or a full page had no successor):
        // go back to the last page that has rows. Only ever backwards, so a total that overstates the rows
        // cannot make it reload forever.
        if (Items.Count == 0 && PageIndex > 0 && PagingMode == PagingMode.Offset && TotalCount is not 0)
        {
            var lastPage = TotalCount == null ? PageIndex - 1 : PageCount!.Value - 1;
            if (TotalCount == null)
            {
                lastKnownPageIndex = lastPage;
            }

            if (lastPage < PageIndex)
            {
                PageIndex = lastPage;
                await OnUserStateChangedAsync();
                _ = LoadAsync();
                return;
            }
        }

        await render();
        if (PageLoaded != null)
        {
            await PageLoaded(page);
        }
    }

    /// <summary>
    /// Fetches another page of the current query (virtual scrolling) without changing the engine's page. Takes
    /// turns with <see cref="LoadAsync"/> at the source, and is canceled by the next load.
    /// </summary>
    public async Task<DataPage<TItem>> FetchPageAsync(PageRequest page, CancellationToken cancellationToken)
    {
        if (Source == null)
        {
            return DataPage<TItem>.Empty;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, fetchCts.Token);
        var request = BuildRequest(page);
        await sourceGate.WaitAsync(linked.Token);
        DataPage<TItem> result;
        try
        {
            result = await Source.GetPageAsync(request, linked.Token);
        }
        finally
        {
            sourceGate.Release();
        }

        // A source that ignores the token still must not hand rows of a replaced query to the new one.
        linked.Token.ThrowIfCancellationRequested();
        return result;
    }

    /// <summary>Called for each keystroke in the search box: applies the search after <see cref="SearchDelay"/> ms of quiet.</summary>
    public Task OnSearchInputAsync(string? text)
    {
        SearchInput = text ?? string.Empty;
        searchDelayCts?.Cancel();
        if (SearchDelay <= 0)
        {
            return SetSearchAsync(SearchInput);
        }

        var cts = searchDelayCts = new CancellationTokenSource();
        _ = DelayedSearchAsync(SearchInput, cts.Token);
        return Task.CompletedTask;
    }

    private async Task DelayedSearchAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(SearchDelay), timeProvider, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await dispatch(() => SetSearchAsync(text));
    }

    /// <summary>Applies search text at once, resets to the first page and reloads.</summary>
    public async Task SetSearchAsync(string? text)
    {
        searchDelayCts?.Cancel();
        text = string.IsNullOrWhiteSpace(text) ? null : text;
        SearchInput = text ?? string.Empty;
        if (string.Equals(text, SearchText, StringComparison.Ordinal))
        {
            return;
        }

        if (!await CanChangeStateAsync())
        {
            SearchInput = SearchText ?? string.Empty;
            await render();
            return;
        }

        SearchText = text;
        ResetPaging();
        await OnUserStateChangedAsync();
        await LoadAsync();
    }

    /// <summary>
    /// Keeps only filters on filterable columns with an allowed operator and values that convert to the column's
    /// type (one per column), so state from a URL or a client cannot reach a source with arbitrary fields.
    /// </summary>
    public IReadOnlyList<FilterSpec> SanitizeFilters(IEnumerable<FilterSpec>? filters)
    {
        if (filters == null)
        {
            return [];
        }

        if (Columns.Count == 0)
        {
            // Not collected yet: SetColumns checks them again.
            return filters.ToList();
        }

        var result = new List<FilterSpec>();
        foreach (var filter in filters)
        {
            var column = Columns.FirstOrDefault(c => c.Id == filter.Column);
            if (column == null || !column.AllowedFilterOperators.Contains(filter.Operator) || result.Any(f => f.Column == filter.Column))
            {
                logger.LogWarning("DataView ignored a filter on '{Column}' with operator {Operator}.", filter.Column, filter.Operator);
                continue;
            }

            var normalized = DataFilters.Normalize(filter, column.Info);
            if (normalized != null)
            {
                result.Add(normalized);
            }
        }

        return result;
    }

    /// <summary>Sets one column's filter (null clears it) and reloads.</summary>
    public Task SetFilterAsync(string columnId, FilterSpec? filter)
    {
        var filters = Filters.Where(f => f.Column != columnId).ToList();
        if (filter != null)
        {
            filters.Add(filter with { Column = columnId });
        }

        return SetFiltersAsync(filters);
    }

    public async Task SetFiltersAsync(IReadOnlyList<FilterSpec> filters)
    {
        var sanitized = SanitizeFilters(filters);
        if (sanitized.SequenceEqual(Filters, FilterComparer.Instance))
        {
            return;
        }

        if (!await CanChangeStateAsync())
        {
            return;
        }

        Filters = sanitized;
        ResetPaging();
        await OnUserStateChangedAsync();
        await LoadAsync();
    }

    /// <summary>Clears the search and all filters.</summary>
    public async Task ClearSearchAndFiltersAsync()
    {
        if (!await CanChangeStateAsync())
        {
            return;
        }

        searchDelayCts?.Cancel();
        SearchText = null;
        SearchInput = string.Empty;
        Filters = [];
        ResetPaging();
        await OnUserStateChangedAsync();
        await LoadAsync();
    }

    /// <summary>Sorts by a column: ascending first, then toggling.</summary>
    public Task ToggleSortAsync(IDataViewColumn<TItem> column)
    {
        var descending = Sort?.Column == column.Id
            ? !Sort.Descending
            : column.DefaultSort == SortOrder.Descending;
        return SetSortAsync(column.Id, descending);
    }

    public async Task SetSortAsync(string? columnId, bool descending)
    {
        if (!await CanChangeStateAsync())
        {
            return;
        }

        if (columnId == null)
        {
            Sort = null;
        }
        else
        {
            if (!Columns.Any(c => c.Sortable && c.Id == columnId))
            {
                throw new ArgumentException($"DataView has no sortable column with Id '{columnId}'.", nameof(columnId));
            }

            Sort = new SortSpec(columnId, descending);
        }

        ResetPaging();
        await OnUserStateChangedAsync();
        await LoadAsync();
    }

    public async Task GoToPageAsync(int pageIndex)
    {
        pageIndex = Math.Max(0, pageIndex);
        if (PagingMode == PagingMode.Cursor)
        {
            // Only visited pages and the next one have a known cursor.
            pageIndex = Math.Min(pageIndex, cursors.Count - 1);
            if (pageIndex > 0 && pageIndex == cursors.Count - 1 && cursors[pageIndex] == null)
            {
                pageIndex--;
            }
        }
        else if (PageCount is { } count)
        {
            pageIndex = Math.Min(pageIndex, count - 1);
        }

        if (pageIndex == PageIndex && HasLoaded)
        {
            return;
        }

        if (!await CanChangeStateAsync())
        {
            return;
        }

        PageIndex = pageIndex;
        await OnUserStateChangedAsync();
        await LoadAsync();
    }

    public async Task ChangePageSizeAsync(int pageSize)
    {
        if (pageSize == PageSize || !await CanChangeStateAsync())
        {
            return;
        }

        SetPageSize(pageSize);
        await OnUserStateChangedAsync();
        await LoadAsync();
    }

    private Task OnUserStateChangedAsync() => StateChanged?.Invoke() ?? Task.CompletedTask;

    public DataViewState GetState() => new()
    {
        Page = PageIndex + 1,
        PageSize = PageSize,
        Sorts = Sort == null ? [] : [Sort],
        Search = SearchText,
        Filters = Filters
    };

    /// <summary>
    /// Applies a state, ignoring sorts on unknown or non-sortable columns. With cursor paging only the first page
    /// can be restored. Does not load.
    /// </summary>
    public void ApplyState(DataViewState state)
    {
        if (state.PageSize > 0)
        {
            PageSize = state.PageSize;
        }

        var sort = state.Sorts.FirstOrDefault();
        if (sort == null)
        {
            Sort = null;
        }
        else if (Columns.Count == 0 || Columns.Any(c => c.Sortable && c.Id == sort.Column))
        {
            Sort = sort;
        }
        else
        {
            logger.LogWarning("DataView state sorts on unknown column '{Column}'; ignored.", sort.Column);
        }

        SearchText = string.IsNullOrWhiteSpace(state.Search) ? null : state.Search;
        SearchInput = SearchText ?? string.Empty;
        Filters = SanitizeFilters(state.Filters);
        ResetPaging();
        if (PagingMode == PagingMode.Offset)
        {
            PageIndex = Math.Max(0, state.Page - 1);
        }
    }

    private sealed class FilterComparer : IEqualityComparer<FilterSpec>
    {
        public static FilterComparer Instance { get; } = new();

        public bool Equals(FilterSpec? x, FilterSpec? y) =>
            new DataViewState { Filters = x == null ? [] : [x] }.Equals(new DataViewState { Filters = y == null ? [] : [y] });

        public int GetHashCode(FilterSpec obj) => HashCode.Combine(obj.Column, obj.Operator);
    }

    public void Dispose()
    {
        loadCts?.Cancel();
        fetchCts.Cancel();
        searchDelayCts?.Cancel();
    }
}
