#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace TabBlazor;

public partial class DataView<TItem>
{
    private readonly Dictionary<string, CancellationTokenSource> filterDelays = [];
    private IReadOnlyList<FilterSpec>? lastFiltersParameter;

    /// <summary>Shows the "Filters (n)" button. Defaults to true when any column is filterable.</summary>
    [Parameter] public bool? ShowFilters { get; set; }

    /// <summary>Adds a row of removable chips for the active filters.</summary>
    [Parameter] public bool ShowFilterChips { get; set; }

    /// <summary>A filter icon per column header (default), or a row of filter controls under the headers.</summary>
    [Parameter] public DataFilterMode FilterMode { get; set; }

    /// <summary>The column filters. Use <c>@bind-Filters</c> to set, save or restore them (for example as presets).</summary>
    [Parameter] public IReadOnlyList<FilterSpec>? Filters { get; set; }

    [Parameter] public EventCallback<IReadOnlyList<FilterSpec>> FiltersChanged { get; set; }

    internal IReadOnlyList<IDataViewColumn<TItem>> FilterableColumns => engine.Columns.Where(c => c.Filterable).ToList();

    internal bool FiltersButtonVisible => ShowFilters ?? engine.Columns.Any(c => c.Filterable);

    internal int ActiveFilterCount => engine.Filters.Count;

    internal bool FiltersPanelOpen { get; private set; }

    internal void SetFiltersPanelOpen(bool open)
    {
        FiltersPanelOpen = open;
        StateHasChanged();
    }

    /// <summary>The active filter of a column, or null.</summary>
    public FilterSpec? GetFilter(string columnId) => engine.Filters.FirstOrDefault(f => f.Column == columnId);

    /// <summary>Sets one column's filter (null clears it) and reloads.</summary>
    public Task SetColumnFilterAsync(string columnId, FilterSpec? filter)
    {
        if (filterDelays.Remove(columnId, out var pending))
        {
            pending.Cancel();
        }

        return engine.SetFilterAsync(columnId, filter);
    }

    /// <summary>Applies a text filter after the same pause as the search box.</summary>
    internal void SetColumnFilterDelayed(string columnId, FilterSpec? filter)
    {
        if (filterDelays.Remove(columnId, out var pending))
        {
            pending.Cancel();
        }

        if (SearchDelay <= 0)
        {
            _ = InvokeAsync(() => engine.SetFilterAsync(columnId, filter));
            return;
        }

        var cts = new CancellationTokenSource();
        filterDelays[columnId] = cts;
        _ = ApplyFilterAfterDelayAsync(columnId, filter, cts);
    }

    private async Task ApplyFilterAfterDelayAsync(string columnId, FilterSpec? filter, CancellationTokenSource cts)
    {
        var timeProvider = Services.GetService<TimeProvider>() ?? TimeProvider.System;
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(SearchDelay), timeProvider, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await InvokeAsync(async () =>
        {
            // Cleared, replaced or disposed while waiting for the dispatcher.
            if (cts.IsCancellationRequested || disposed)
            {
                return;
            }

            filterDelays.Remove(columnId);
            await engine.SetFilterAsync(columnId, filter);
        });
    }

    /// <summary>Drops typed filters that have not been applied yet, so they cannot come back after a clear.</summary>
    private void CancelFilterDelays()
    {
        foreach (var pending in filterDelays.Values)
        {
            pending.Cancel();
        }

        filterDelays.Clear();
    }

    internal DataFilterContext<TItem> GetFilterContext(IDataViewColumn<TItem> column) => new(this, column);

    // A changed Filters parameter replaces the filters (it is not a user action, so StateChanged is not raised).
    private bool OnParametersSetFilters()
    {
        if (Filters == null || ReferenceEquals(Filters, lastFiltersParameter))
        {
            return false;
        }

        lastFiltersParameter = Filters;
        if (new DataViewState { Filters = Filters }.Equals(new DataViewState { Filters = engine.Filters }))
        {
            return false;
        }

        CancelFilterDelays();
        engine.ApplyState(engine.GetState() with { Page = 1, Filters = Filters });
        return true;
    }

    private async Task NotifyFiltersChangedAsync(IReadOnlyList<FilterSpec> before)
    {
        if (!new DataViewState { Filters = before }.Equals(new DataViewState { Filters = engine.Filters }))
        {
            lastFiltersParameter = engine.Filters;
            await FiltersChanged.InvokeAsync(engine.Filters);
        }
    }
}
