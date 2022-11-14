using Microsoft.AspNetCore.Components.Web.Virtualization;
using TabBlazor.Components.QuickTables.Infrastructure;

namespace TabBlazor.Components.QuickTables;

[CascadingTypeParameter(nameof(TGridItem))]
public partial class QuickTable<TGridItem> : IAsyncDisposable
{
    private readonly EventCallbackSubscriber<PaginationState> currentPageItemsChanged;

    private readonly RenderFragment renderColumnHeaders;
    private readonly RenderFragment renderNonVirtualizedRows;
    private int ariaBodyRowCount;

    private IAsyncQueryExecutor asyncQueryExecutor;
    private bool collectingColumns;
    private List<ColumnBase<TGridItem>> columns;
    private ICollection<TGridItem> currentNonVirtualizedViewItems = Array.Empty<TGridItem>();

    private ColumnBase<TGridItem> displayOptionsForColumn;

    private InternalGridContext<TGridItem> internalGridContext;
    private object lastAssignedItemsOrProvider;

    private int? lastRefreshedPaginationStateHash;
    private CancellationTokenSource pendingDataLoadCancellationTokenSource;

    private ElementReference tableReference;
    private Virtualize<(int, TGridItem)> VirtualizeComponent;

    public QuickTable()
    {
        columns = new List<ColumnBase<TGridItem>>();
        internalGridContext = new InternalGridContext<TGridItem>(this);
        currentPageItemsChanged =
            new EventCallbackSubscriber<PaginationState>(
                EventCallback.Factory.Create<PaginationState>(this, RefreshDataCoreAsync));
        renderColumnHeaders = RenderColumnHeaders;
        renderNonVirtualizedRows = RenderNonVirtualizedRows;

        var columnsFirstCollectedSubscriber = new EventCallbackSubscriber<object>(
            EventCallback.Factory.Create<object>(this, RefreshDataCoreAsync));
        columnsFirstCollectedSubscriber.SubscribeOrMove(internalGridContext.ColumnsFirstCollected);
    }

    [Parameter] public IQueryable<TGridItem> Items { get; set; }
    [Parameter] public GridItemsProvider<TGridItem> ItemsProvider { get; set; }
    [Parameter] public string Class { get; set; }
    [Parameter] public string Theme { get; set; } = "default";
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public bool Virtualize { get; set; }
    [Parameter] public float ItemSize { get; set; } = 50;
    [Parameter] public bool ResizableColumns { get; set; }
    [Parameter] public Func<TGridItem, object> ItemKey { get; set; } = x => x!;
    [Parameter] public PaginationState Pagination { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = default!;
    public ColumnBase<TGridItem> SortByColumn { get; set; }
    public bool SortByAscending { get; set; }

    public ValueTask DisposeAsync()
    {
        currentPageItemsChanged.Dispose();
        return ValueTask.CompletedTask;
    }

    protected override Task OnParametersSetAsync()
    {
        // The associated pagination state may have been added/removed/replaced
        currentPageItemsChanged.SubscribeOrMove(Pagination?.CurrentPageItemsChanged);

        if (Items is not null && ItemsProvider is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(QuickTables)} requires one of {nameof(Items)} or {nameof(ItemsProvider)}, but both were specified.");
        }

        // Perform a re-query only if the data source or something else has changed
        var _newItemsOrItemsProvider = Items ?? (object)ItemsProvider;
        var dataSourceHasChanged = _newItemsOrItemsProvider != lastAssignedItemsOrProvider;
        if (dataSourceHasChanged)
        {
            lastAssignedItemsOrProvider = _newItemsOrItemsProvider;
            asyncQueryExecutor = AsyncQueryExecutorSupplier.GetAsyncQueryExecutor(Services, Items);
        }

        var mustRefreshData = dataSourceHasChanged
                              || Pagination?.GetHashCode() != lastRefreshedPaginationStateHash;

        return columns.Count > 0 && mustRefreshData ? RefreshDataCoreAsync() : Task.CompletedTask;
    }

    internal void AddColumn(ColumnBase<TGridItem> column, SortDirection? isDefaultSortDirection)
    {
        if (collectingColumns)
        {
            columns.Add(column);

            if (SortByColumn is null && isDefaultSortDirection.HasValue)
            {
                SortByColumn = column;
                SortByAscending = isDefaultSortDirection.Value != SortDirection.Descending;
            }
        }
    }

    private void StartCollectingColumns()
    {
        columns.Clear();
        collectingColumns = true;
    }

    private void FinishCollectingColumns()
    {
        collectingColumns = false;
    }

    public Task SortByColumnAsync(ColumnBase<TGridItem> column, SortDirection direction = SortDirection.Auto)
    {
        SortByAscending = direction switch
        {
            SortDirection.Ascending => true,
            SortDirection.Descending => false,
            SortDirection.Auto => SortByColumn == column ? !SortByAscending : true,
            _ => throw new NotSupportedException($"Unknown sort direction {direction}")
        };

        SortByColumn = column;

        StateHasChanged(); // We want to see the updated sort order in the header, even before the data query is completed
        return RefreshDataAsync();
    }

    public void ShowColumnOptions(ColumnBase<TGridItem> column)
    {
        displayOptionsForColumn = column;
        StateHasChanged();
    }

    public async Task RefreshDataAsync()
    {
        await RefreshDataCoreAsync();
        StateHasChanged();
    }

    // Same as RefreshDataAsync, except without forcing a re-render. We use this from OnParametersSetAsync
    // because in that case there's going to be a re-render anyway.
    private async Task RefreshDataCoreAsync()
    {
        // Move into a "loading" state, cancelling any earlier-but-still-pending load
        pendingDataLoadCancellationTokenSource?.Cancel();
        var thisLoadCts = pendingDataLoadCancellationTokenSource = new CancellationTokenSource();

        if (VirtualizeComponent is not null)
        {
            // If we're using Virtualize, we have to go through its RefreshDataAsync API otherwise:
            // (1) It won't know to update its own internal state if the provider output has changed
            // (2) We won't know what slice of data to query for
            await VirtualizeComponent.RefreshDataAsync();
            pendingDataLoadCancellationTokenSource = null;
        }
        else
        {
            // If we're not using Virtualize, we build and execute a request against the items provider directly
            lastRefreshedPaginationStateHash = Pagination?.GetHashCode();
            var startIndex = Pagination is null ? 0 : Pagination.CurrentPageIndex * Pagination.ItemsPerPage;
            var request = new GridItemsProviderRequest<TGridItem>(
                startIndex, Pagination?.ItemsPerPage, SortByColumn, SortByAscending, thisLoadCts.Token);
            var result = await ResolveItemsRequestAsync(request);
            if (!thisLoadCts.IsCancellationRequested)
            {
                currentNonVirtualizedViewItems = result.Items;
                ariaBodyRowCount = currentNonVirtualizedViewItems.Count;
                Pagination?.SetTotalItemCountAsync(result.TotalItemCount);
                pendingDataLoadCancellationTokenSource = null;
            }
        }
    }

    // Gets called both by RefreshDataCoreAsync and directly by the Virtualize child component during scrolling
    private async ValueTask<ItemsProviderResult<(int, TGridItem)>> ProvideVirtualizedItems(ItemsProviderRequest request)
    {
        lastRefreshedPaginationStateHash = Pagination?.GetHashCode();

        // Debounce the requests. This eliminates a lot of redundant queries at the cost of slight lag after interactions.
        // TODO: Consider making this configurable, or smarter (e.g., doesn't delay on first call in a batch, then the amount
        // of delay increases if you rapidly issue repeated requests, such as when scrolling a long way)
        await Task.Delay(100);
        if (request.CancellationToken.IsCancellationRequested)
        {
            return default;
        }

        // Combine the query parameters from Virtualize with the ones from PaginationState
        var startIndex = request.StartIndex;
        var count = request.Count;
        if (Pagination is not null)
        {
            startIndex += Pagination.CurrentPageIndex * Pagination.ItemsPerPage;
            count = Math.Min(request.Count, Pagination.ItemsPerPage - request.StartIndex);
        }

        var providerRequest = new GridItemsProviderRequest<TGridItem>(
            startIndex, count, SortByColumn, SortByAscending, request.CancellationToken);
        var providerResult = await ResolveItemsRequestAsync(providerRequest);

        if (!request.CancellationToken.IsCancellationRequested)
        {
            // ARIA's rowcount is part of the UI, so it should reflect what the human user regards as the number of rows in the table,
            // not the number of physical <tr> elements. For virtualization this means what's in the entire scrollable range, not just
            // the current viewport. In the case where you're also paginating then it means what's conceptually on the current page.
            // TODO: This currently assumes we always want to expand the last page to have ItemsPerPage rows, but the experience might
            //       be better if we let the last page only be as big as its number of actual rows.
            ariaBodyRowCount = Pagination is null ? providerResult.TotalItemCount : Pagination.ItemsPerPage;

            Pagination?.SetTotalItemCountAsync(providerResult.TotalItemCount);

            // We're supplying the row index along with each row's data because we need it for aria-rowindex, and we have to account for
            // the virtualized start index. It might be more performant just to have some _latestQueryRowStartIndex field, but we'd have
            // to make sure it doesn't get out of sync with the rows being rendered.
            return new ItemsProviderResult<(int, TGridItem)>(
                providerResult.Items.Select((x, i) => ValueTuple.Create(i + request.StartIndex + 2, x)),
                ariaBodyRowCount);
        }

        return default;
    }

    // Normalizes all the different ways of configuring a data source so they have common GridItemsProvider-shaped API
    private async ValueTask<GridItemsProviderResult<TGridItem>> ResolveItemsRequestAsync(
        GridItemsProviderRequest<TGridItem> request)
    {
        if (ItemsProvider is not null)
        {
            return await ItemsProvider(request);
        }

        if (Items is not null)
        {
            var totalItemCount = asyncQueryExecutor is null
                ? Items.Count()
                : await asyncQueryExecutor.CountAsync(Items);
            var result = request.ApplySorting(Items).Skip(request.StartIndex);
            if (request.Count.HasValue)
            {
                result = result.Take(request.Count.Value);
            }

            var resultArray = asyncQueryExecutor is null
                ? result.ToArray()
                : await asyncQueryExecutor.ToArrayAsync(result);
            return GridItemsProviderResult.From(resultArray, totalItemCount);
        }

        return GridItemsProviderResult.From(Array.Empty<TGridItem>(), 0);
    }

    private string AriaSortValue(ColumnBase<TGridItem> column)
    {
        return SortByColumn == column
            ? SortByAscending ? "ascending" : "descending"
            : "none";
    }

    private string ColumnHeaderClass(ColumnBase<TGridItem> column)
    {
        return SortByColumn == column
            ? $"{ColumnClass(column)} {(SortByAscending ? "col-sort-asc" : "col-sort-desc")}"
            : ColumnClass(column);
    }

    private string GridClass()
    {
        return $"table-responsive {Class} {(pendingDataLoadCancellationTokenSource is null ? null : "loading")}";
    }

    private static string ColumnClass(ColumnBase<TGridItem> column)
    {
        return column.Align switch
        {
            Align.Center => $"col-justify-center {column.Class}",
            Align.Right => $"col-justify-end {column.Class}",
            _ => column.Class
        };
    }

    private void CloseColumnOptions()
    {
        displayOptionsForColumn = null;
    }
}