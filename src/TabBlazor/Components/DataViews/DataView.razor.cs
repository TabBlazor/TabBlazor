#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TabBlazor;

/// <summary>
/// Shows a collection of data from local or remote sources, with sort, word-based search, paging, loading,
/// empty and error states. Supply data with <see cref="Items"/> (a local list), <see cref="ItemsProvider"/>
/// (an inline async provider) or <see cref="Source"/> (any <see cref="IDataViewSource{TItem}"/>), and describe
/// the data with <c>DataColumn</c>s in <see cref="ChildContent"/>.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
[CascadingTypeParameter(nameof(TItem))]
public partial class DataView<TItem> : ComponentBase, IAsyncDisposable
{
    private readonly List<IDataViewColumn<TItem>> collectingColumns = [];
    private DataViewContext<TItem> context = default!;
    private DataViewEngine<TItem> engine = default!;
    private bool collecting;
    private bool columnsInitialized;
    private bool initialLoadStarted;
    private object? lastSourceParameter;
    private int lastItemsCount = -1;
    private bool lastNaturalSort;
    private ExpressionKey? lastKeySelector;
    private int lastPageSize;
    private DataViewState? lastState;
    private TableDensity? density;
    private DataViewKind? chosenView;
    private bool disposed;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    // Data

    /// <summary>Local data. Sorting, searching and paging run in memory with the columns' expressions.</summary>
    [Parameter] public IEnumerable<TItem>? Items { get; set; }

    /// <summary>An inline async provider: gets a <see cref="DataRequest"/> and returns a <see cref="DataPage{TItem}"/>.</summary>
    [Parameter] public Func<DataRequest, CancellationToken, Task<DataPage<TItem>>>? ItemsProvider { get; set; }

    /// <summary>A reusable source, such as a remote adapter.</summary>
    [Parameter] public IDataViewSource<TItem>? Source { get; set; }

    /// <summary>The paging mode of <see cref="ItemsProvider"/>. Defaults to Offset.</summary>
    [Parameter] public PagingMode PagingMode { get; set; } = PagingMode.Offset;

    /// <summary>
    /// The row key, for example <c>o =&gt; o.Id</c>. Recommended for remote sources: it keys rows for rendering
    /// and is added as the last sort so offset paging never repeats or skips rows.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? KeySelector { get; set; }

    /// <summary>Sorts text with embedded numbers naturally when using <see cref="Items"/>.</summary>
    [Parameter] public bool NaturalSort { get; set; }

    /// <summary>The columns (<c>DataColumn</c>s).</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    // Views

    /// <summary>The view to show. Use <c>@bind-View</c> to follow the user's choice in the view switcher.</summary>
    [Parameter] public DataViewKind? View { get; set; }

    /// <summary>Raised when the user picks another view in the switcher.</summary>
    [Parameter] public EventCallback<DataViewKind?> ViewChanged { get; set; }

    /// <summary>The views offered in the toolbar's view switcher. The switcher shows when more than one is set.</summary>
    [Parameter] public DataViewKind Views { get; set; } = DataViewKind.Table;


    // Identity and header

    /// <summary>
    /// The view's general id: the HTML id of the card and a stable selector for tests (and, later, the URL prefix
    /// and storage key).
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>The title shown on the left of the header.</summary>
    [Parameter] public string? Title { get; set; }



    [Parameter] public bool ShowHeader { get; set; } = true;

    /// <summary>Shows the search box. Defaults to true when any column is searchable.</summary>
    [Parameter] public bool? ShowSearch { get; set; }

    /// <summary>Shows the refresh button. Defaults to true for remote sources and false for <see cref="Items"/>.</summary>
    [Parameter] public bool? ShowRefresh { get; set; }

    [Parameter] public bool ShowFooter { get; set; } = true;

    /// <summary>Shows the "⋮" menu (density). Defaults to true.</summary>
    [Parameter] public bool ShowMenu { get; set; } = true;

    // Paging and search

    /// <summary>The page size. Defaults to 25.</summary>
    [Parameter] public int PageSize { get; set; } = 25;

    /// <summary>The page sizes offered in the footer. Empty hides the selector.</summary>
    [Parameter] public int[] PageSizes { get; set; } = [10, 25, 50, 100];

    /// <summary>Words (default): every word must match a searchable column. Phrase: the whole text must match one.</summary>
    [Parameter] public SearchMode SearchMode { get; set; } = SearchMode.Words;

    /// <summary>The maximum number of search terms. Defaults to 5.</summary>
    [Parameter] public int MaxSearchTerms { get; set; } = SearchSpec.DefaultMaxTerms;

    /// <summary>Milliseconds to wait after the last keystroke before searching. Defaults to 300.</summary>
    [Parameter] public int SearchDelay { get; set; } = 300;

    // Rows

    /// <summary>Makes each row a real link to this address (Ctrl-click and middle-click open a new tab).</summary>
    [Parameter] public Func<TItem, string?>? RowHref { get; set; }

    /// <summary>Raised when a row is clicked (outside actions, links and inputs).</summary>
    [Parameter] public EventCallback<TItem> OnRowClicked { get; set; }

    /// <summary>A Tabler color per row, for example <c>o =&gt; o.IsOverdue ? TablerColor.Danger : null</c>.</summary>
    [Parameter] public Func<TItem, TablerColor?>? RowColor { get; set; }

    /// <summary>CSS classes per row.</summary>
    [Parameter] public Func<TItem, string?>? RowClass { get; set; }

    /// <summary>Inline style per row.</summary>
    [Parameter] public Func<TItem, string?>? RowStyle { get; set; }

    // Look

    [Parameter] public bool Striped { get; set; }
    [Parameter] public bool Hover { get; set; }
    [Parameter] public bool Bordered { get; set; }

    /// <summary>Row density. When not set, the user can change it in the "⋮" menu.</summary>
    [Parameter] public TableDensity? Density { get; set; }

    /// <summary>Keeps the column headers visible while scrolling. Use with <see cref="Height"/>.</summary>
    [Parameter] public bool StickyHeader { get; set; }

    /// <summary>A fixed height for the scroll area, for example <c>600px</c>.</summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>Extra CSS classes for the <c>table</c> element.</summary>
    [Parameter] public string? TableClass { get; set; }

    /// <summary>Extra CSS classes for the card.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Unknown attributes are passed to the <c>table</c> element.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object>? UnmatchedParameters { get; set; }

    // Templates are filled by slot components placed among the columns: <HeaderTemplate>, <ToolbarTemplate>,
    // <LoadingTemplate>, <EmptyTemplate>, <NoResultsTemplate>, <ErrorTemplate> and <ListItemTemplate>.
    internal RenderFragment? HeaderTemplate { get; private set; }
    internal RenderFragment? ToolbarTemplate { get; private set; }
    internal RenderFragment? LoadingTemplate { get; private set; }
    internal RenderFragment? EmptyTemplate { get; private set; }
    internal RenderFragment? NoResultsTemplate { get; private set; }
    internal RenderFragment<Exception>? ErrorTemplate { get; private set; }
    internal RenderFragment<TItem>? ListItemTemplate { get; private set; }

    // States





    /// <summary>Overrides built-in UI strings.</summary>
    [Parameter] public DataViewText? Text { get; set; }

    // State and events

    /// <summary>Paging, sort, search and filters. Use <c>@bind-State</c> to save, restore or drive them.</summary>
    [Parameter] public DataViewState? State { get; set; }

    /// <summary>Raised when the user changes paging, sort, search or filters.</summary>
    [Parameter] public EventCallback<DataViewState> StateChanged { get; set; }

    /// <summary>Raised after each page has loaded, with the rows and the total.</summary>
    [Parameter] public EventCallback<DataPage<TItem>> OnPageLoaded { get; set; }

    // Public read-only state

    /// <summary>The current paging, sort, search and filters.</summary>
    public DataViewState CurrentState => engine.GetState();

    /// <summary>The rows of the current page.</summary>
    public IReadOnlyList<TItem> CurrentItems => engine.Items;

    /// <summary>The total number of matching rows, or null when the source does not report it.</summary>
    public int? TotalCount => engine.TotalCount;

    public bool IsLoading => engine.IsLoading;

    /// <summary>The error of the last load, or null.</summary>
    public Exception? Error => engine.Error;

    /// <summary>The visible columns.</summary>
    public IReadOnlyList<IDataViewColumn<TItem>> Columns => engine.Columns;

    /// <summary>The view being shown.</summary>
    public DataViewKind CurrentView => SingleView(View ?? chosenView ?? Views);

    /// <summary>What the current view shows: its items, or a loading, empty, no-results or error state.</summary>
    public DataViewStatus Status => engine.Error != null ? DataViewStatus.Error
        : !engine.HasLoaded ? DataViewStatus.FirstLoad
        : engine.Items.Count > 0 ? DataViewStatus.Items
        : engine.HasSearchOrFilters ? DataViewStatus.NoResults
        : DataViewStatus.Empty;

    internal IReadOnlyList<DataViewKind> AvailableViews =>
        new[] { DataViewKind.Table, DataViewKind.List }.Where(v => Views.HasFlag(v)).ToList();

    internal DataViewEngine<TItem> Engine => engine;
    internal DataViewText Texts => Text ?? DataViewText.Default;
    internal TableDensity CurrentDensity => Density ?? density ?? TableDensity.Normal;
    internal IReadOnlyList<IDataViewColumn<TItem>> VisibleColumns =>
        engine.Columns.Where(c => c.Visible && c.ShowIn != ColumnVisibility.EditOnly).ToList();

    /// <summary>The columns shown in edit forms.</summary>
    internal IReadOnlyList<IDataViewColumn<TItem>> FormColumns =>
        engine.Columns.Where(c => c.ShowIn != ColumnVisibility.ViewOnly).ToList();

    internal ILogger Logger { get; private set; } = NullLogger.Instance;
    internal bool SearchVisible => ShowSearch ?? engine.Columns.Any(c => c.Searchable);
    internal bool RefreshVisible => ShowRefresh ?? Items == null;

    protected override void OnInitialized()
    {
        var timeProvider = Services.GetService<TimeProvider>() ?? TimeProvider.System;
        Logger = (ILogger?)Services.GetService<ILoggerFactory>()?.CreateLogger("TabBlazor.DataView") ?? NullLogger.Instance;
        engine = new DataViewEngine<TItem>(
            work => InvokeAsync(work),
            () => disposed ? Task.CompletedTask : InvokeAsync(StateHasChanged),
            timeProvider,
            Logger)
        {
            StateChanged = OnEngineStateChangedAsync,
            BeforeUserStateChange = CanChangeStateAsync,
            PageLoaded = page =>
            {
                OnPageLoadedInteraction(page);
                ResetVirtualCache(page);
                return OnPageLoaded.InvokeAsync(page);
            }
        };
        context = new DataViewContext<TItem>(this);
        OnInitializedUrlState();
    }

    protected override async Task OnParametersSetAsync()
    {
        var sourceCount = (Items != null ? 1 : 0) + (ItemsProvider != null ? 1 : 0) + (Source != null ? 1 : 0);
        if (sourceCount != 1)
        {
            throw new InvalidOperationException(
                $"DataView needs exactly one of {nameof(Items)}, {nameof(ItemsProvider)} or {nameof(Source)}.");
        }

        engine.SearchMode = SearchMode;
        engine.MaxSearchTerms = MaxSearchTerms;
        engine.SearchDelay = SearchDelay;

        var reload = false;

        var sourceParameter = (object?)Items ?? (object?)ItemsProvider ?? Source;
        var itemsCount = Items is ICollection<TItem> collection ? collection.Count : Items is IReadOnlyCollection<TItem> ro ? ro.Count : -1;
        if (!ReferenceEquals(sourceParameter, lastSourceParameter) || NaturalSort != lastNaturalSort ||
            (ItemsProvider != null && engine.Source?.PagingMode != PagingMode))
        {
            engine.Source = Items != null
                ? new InMemorySource<TItem>(Items, new InMemorySourceOptions { NaturalSort = NaturalSort })
                : ItemsProvider != null
                    ? new DelegateDataViewSource<TItem>(ItemsProvider, PagingMode)
                    : Source;
            engine.SortByKeyWhenUnsorted = Items == null;
            if (lastSourceParameter != null)
            {
                engine.ResetPaging();
                reload = true;
            }

            lastSourceParameter = sourceParameter;
            lastNaturalSort = NaturalSort;
        }
        else if (Items != null && itemsCount != lastItemsCount)
        {
            reload = true;
        }

        lastItemsCount = itemsCount;

        var keyKey = ExpressionKey.Create(KeySelector);
        if (!Equals(keyKey, lastKeySelector))
        {
            lastKeySelector = keyKey;
            engine.SetKey(KeySelector);
        }

        if (PageSize != lastPageSize)
        {
            lastPageSize = PageSize;
            engine.SetPageSize(PageSize);
            reload = true;
        }

        if (OnParametersSetFilters())
        {
            reload = true;
        }

        if (State != null && !State.Equals(lastState))
        {
            lastState = State;
            engine.ApplyState(State);
            reload = true;
        }

        OnParametersSetInteraction();

        // A parent re-render may have changed templates or styling functions.
        engine.InvalidateRows();

        if (reload && initialLoadStarted)
        {
            await engine.LoadAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await WatchScreenSizeAsync();
        await ApplyVirtualResetAsync();
        await AttachColumnResizeAsync();
        await NotifyUrlStateAsync();

        if (firstRender && columnsInitialized && !initialLoadStarted)
        {
            // Remote sources load after the first interactive render, so prerendering does not query twice.
            initialLoadStarted = true;
            await engine.LoadAsync();
        }
    }

    internal void StartCollectingColumns()
    {
        collectingColumns.Clear();
        collecting = true;
    }

    internal void AddColumn(IDataViewColumn<TItem> column)
    {
        if (collecting && !collectingColumns.Contains(column))
        {
            collectingColumns.Add(column);
        }
    }

    internal void SetSlot(DataViewSlot slot, object? template)
    {
        switch (slot)
        {
            case DataViewSlot.Header: HeaderTemplate = (RenderFragment?)template; break;
            case DataViewSlot.Toolbar: ToolbarTemplate = (RenderFragment?)template; break;
            case DataViewSlot.Loading: LoadingTemplate = (RenderFragment?)template; break;
            case DataViewSlot.Empty: EmptyTemplate = (RenderFragment?)template; break;
            case DataViewSlot.NoResults: NoResultsTemplate = (RenderFragment?)template; break;
            case DataViewSlot.Error: ErrorTemplate = (RenderFragment<Exception>?)template; break;
            case DataViewSlot.ListItem:
                if (!ReferenceEquals(ListItemTemplate, template))
                {
                    engine.InvalidateRows();
                }

                ListItemTemplate = (RenderFragment<TItem>?)template;
                break;
            default:
                SetInteractionSlot(slot, template);
                break;
        }
    }

    internal void FinishCollectingColumns()
    {
        collecting = false;
        var changed = collectingColumns.Count != engine.Columns.Count ||
                      collectingColumns.Where((c, i) => !ReferenceEquals(c, engine.Columns[i]) || c.Resolve(i) != engine.Columns[i].Info).Any();
        if (!changed)
        {
            return;
        }

        engine.SetColumns(collectingColumns.ToList());
        engine.InvalidateRows();
        if (!columnsInitialized)
        {
            columnsInitialized = true;
            if (State == null || State.Sorts.Count == 0)
            {
                engine.ApplyDefaultSort();
            }

            ApplyUrlStateOnFirstCollect();
        }
    }

    // Called once, while the first render batch is still running.
    internal async Task OnColumnsCollectedAsync()
    {
        if (!initialLoadStarted && engine.Source is InMemorySource<TItem>)
        {
            // Local data loads synchronously during the first render, so the first paint (and prerendering) has rows.
            initialLoadStarted = true;
            await engine.LoadAsync();
        }
    }

    private async Task OnEngineStateChangedAsync()
    {
        OnUserStateChangedInteraction();
        lastState = engine.GetState();
        await StateChanged.InvokeAsync(lastState);
        await NotifyFiltersChangedAsync(lastFiltersParameter ?? []);
        WriteUrlState();
    }

    private static DataViewKind SingleView(DataViewKind views) =>
        views.HasFlag(DataViewKind.Table) || views == DataViewKind.None ? DataViewKind.Table : DataViewKind.List;

    internal async Task SwitchViewAsync(DataViewKind view)
    {
        if (view == CurrentView)
        {
            return;
        }

        chosenView = view;
        resizeAttached = false;
        engine.InvalidateRows();
        WriteUrlState();
        await ViewChanged.InvokeAsync(view);
        StateHasChanged();
    }

    internal void SetDensity(TableDensity value)
    {
        density = value;
        engine.InvalidateRows();
        StateHasChanged();
    }

    internal object? GetRowKey(TItem item) => engine.KeyGetter != null ? engine.KeyGetter(item) : item;

    internal string? GetRowClass(TItem item, bool table = true)
    {
        var state = GetRowState(item);
        var builder = new ClassBuilder(DataViewStyles.ColorClass(RowColor?.Invoke(item), table));
        builder.Add(RowClass?.Invoke(item));
        if (RowHref != null && table)
        {
            builder.Add("dv-row-link");
        }

        if (OnRowClicked.HasDelegate || RowClick is DataRowClick.Details or DataRowClick.Select ||
            (RowClick == DataRowClick.Auto && (HasDetails || SelectionMode == DataSelectionMode.Single)))
        {
            builder.Add("cursor-pointer");
        }

        builder.AddIf(table ? "table-active" : "active", state.Selected || state.Current)
            .AddIf("dv-row-saved", state.JustSaved)
            .AddIf("dv-row-expanded", state.Expanded);

        var css = builder.ToString();
        return string.IsNullOrWhiteSpace(css) ? null : css;
    }

    internal string GetCardClass() => new ClassBuilder("card dataview")
        .AddIf("dv-small", IsSmallScreen)
        .Add(CurrentView == DataViewKind.Table ? $"dv-{EffectiveMobileLayout.ToString().ToLowerInvariant()}-{Breakpoint}" : null)
        .Add(Class)
        .ToString();

    internal string GetTableClass() => new ClassBuilder("table card-table table-vcenter dv-table")
        .AddIf("table-striped", Striped)
        .AddIf("table-hover", Hover || RowHref != null || OnRowClicked.HasDelegate || HasDetails || SelectionMode == DataSelectionMode.Single)
        .AddIf("table-bordered", Bordered)
        .AddIf("table-sm", CurrentDensity == TableDensity.Compact)
        .AddIf("dv-comfortable", CurrentDensity == TableDensity.Comfortable)
        .AddIf("dv-sticky-header", StickyHeader || IsVirtualized)
        .AddIf("dv-virtual", IsVirtualized)
        .Add(TableClass)
        .ToString();

    // Public API

    /// <summary>Reloads the current page, for example after an external change.</summary>
    public Task ReloadAsync()
    {
        engine.InvalidateRows();
        initialLoadStarted = true;
        return engine.LoadAsync();
    }

    /// <summary>Re-renders all rows without reloading, for styling that depends on something outside the row.</summary>
    public void Refresh()
    {
        engine.InvalidateRows();
        StateHasChanged();
    }

    /// <summary>Goes to a 1-based page.</summary>
    public Task GoToPageAsync(int page) => engine.GoToPageAsync(page - 1);

    /// <summary>Applies search text at once (no delay) and goes to the first page.</summary>
    public Task SetSearchAsync(string? text) => engine.SetSearchAsync(text);

    /// <summary>Sorts by a column id, or clears the sort when <paramref name="columnId"/> is null.</summary>
    public Task SetSortAsync(string? columnId, bool descending = false) => engine.SetSortAsync(columnId, descending);

    /// <summary>Replaces the column filters.</summary>
    public Task SetFiltersAsync(IEnumerable<FilterSpec> filters) => engine.SetFiltersAsync(filters.ToList());

    /// <summary>Removes all column filters.</summary>
    public Task ClearFiltersAsync() => engine.SetFiltersAsync([]);

    internal Task ClearSearchAndFiltersAsync() => engine.ClearSearchAndFiltersAsync();

    public async ValueTask DisposeAsync()
    {
        disposed = true;
        engine?.Dispose();
        await DisposeResponsiveAsync();
        DisposeUrlState();
    }
}

/// <summary>Cascaded (fixed) to columns and views of a DataView.</summary>
internal sealed class DataViewContext<TItem>(DataView<TItem> view) : IDataViewSlotHost
{
    public DataView<TItem> View { get; } = view;

    public void AddColumn(IDataViewColumn<TItem> column) => View.AddColumn(column);

    public void SetSlot(DataViewSlot slot, object? template) => View.SetSlot(slot, template);
}

/// <summary>Renders its content after the components before it, so the view renders after its columns registered.</summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class DataViewDefer : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);
}

/// <summary>Notifies the view once, during the first render batch, that its columns have been collected.</summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class DataViewColumnsCollected : IComponent
{
    private bool fired;

    [Parameter] public Func<Task>? OnCollected { get; set; }

    public void Attach(RenderHandle renderHandle)
    {
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        if (fired)
        {
            return Task.CompletedTask;
        }

        fired = true;
        parameters.SetParameterProperties(this);
        return OnCollected?.Invoke() ?? Task.CompletedTask;
    }
}
