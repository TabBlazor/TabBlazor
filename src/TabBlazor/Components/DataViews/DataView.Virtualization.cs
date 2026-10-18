#nullable enable
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.Extensions.Logging;
using TabBlazor.Services;

namespace TabBlazor;

public partial class DataView<TItem>
{
    // Rows fetched while virtualized, by index. Cleared when sort, search or filters change.
    private readonly Dictionary<int, TItem> virtualCache = [];
    private readonly SemaphoreSlim virtualGate = new(1, 1);
    private int? virtualTotal;
    private string? virtualNextCursor;
    private bool virtualExhausted;
    private bool virtualResetPending;
    private int virtualGeneration;

    [Inject] private TablerService TablerService { get; set; } = default!;

    /// <summary>
    /// Shows one scrolling list instead of pages, rendering only the visible rows. Offset sources with a total get
    /// true virtualization; cursor sources (and sources without a total) scroll infinitely. Rows need a fixed height
    /// (<see cref="ItemSize"/>) and the view a <see cref="Height"/>.
    /// </summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>The fixed row height in pixels while virtualized. Defaults to 44.</summary>
    [Parameter] public float ItemSize { get; set; } = 44;

    /// <summary>How many extra rows render above and below the visible ones while virtualized. Defaults to 3.</summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    internal bool IsVirtualized => Virtualize;

    internal Virtualize<TItem>? VirtualizeComponent { get; set; }

    internal ElementReference ScrollContainer { get; set; }

    /// <summary>The number of rows in the scrolling list: the total, or the loaded rows when the total is unknown.</summary>
    internal int VirtualRowCount => virtualTotal ?? virtualCache.Count;

    // A new first page (sort, search, filters or reload): start the list over from the top.
    private void ResetVirtualCache(DataPage<TItem> page)
    {
        if (!IsVirtualized)
        {
            return;
        }

        virtualGeneration++;
        virtualCache.Clear();
        for (var i = 0; i < page.Items.Count; i++)
        {
            virtualCache[i] = page.Items[i];
        }

        virtualTotal = page.TotalCount;
        virtualNextCursor = page.NextCursor;
        virtualExhausted = engine.PagingMode == PagingMode.Cursor
            ? page.NextCursor == null
            : page.TotalCount is { } total ? page.Items.Count >= total : page.Items.Count < engine.PageSize;
        virtualResetPending = true;

        // The footer shows the row count.
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task ApplyVirtualResetAsync()
    {
        if (!virtualResetPending || VirtualizeComponent == null)
        {
            return;
        }

        virtualResetPending = false;
        await VirtualizeComponent.RefreshDataAsync();
        try
        {
            await TablerService.SetElementProperty(ScrollContainer, "scrollTop", 0);
        }
        catch (Exception ex) when (ex is Microsoft.JSInterop.JSException or InvalidOperationException or TaskCanceledException)
        {
        }
    }

    internal void ReplaceCachedItem(Func<TItem, bool> match, TItem item)
    {
        foreach (var index in virtualCache.Where(p => match(p.Value)).Select(p => p.Key).ToList())
        {
            virtualCache[index] = item;
        }
    }

    /// <summary>Supplies rows to Blazor's Virtualize: from the cache, fetching missing ranges (one fetch at a time).</summary>
    internal async ValueTask<ItemsProviderResult<TItem>> ProvideVirtualItemsAsync(ItemsProviderRequest request)
    {
        if (!engine.HasLoaded || engine.Error != null || engine.Items.Count == 0)
        {
            return new ItemsProviderResult<TItem>([], 0);
        }

        var start = request.StartIndex;
        var end = start + request.Count;
        var generation = virtualGeneration;
        var sequential = engine.PagingMode == PagingMode.Cursor || virtualTotal == null;

        await virtualGate.WaitAsync(request.CancellationToken);
        try
        {
            if (sequential)
            {
                // Infinite scroll: load the next pages in order until the requested range is covered.
                while (generation == virtualGeneration && !virtualExhausted && virtualCache.Count < end)
                {
                    await FetchNextVirtualPageAsync(request.CancellationToken);
                }
            }
            else
            {
                var total = virtualTotal!.Value;
                end = Math.Min(end, total);
                var missing = Enumerable.Range(start, Math.Max(0, end - start)).Where(i => !virtualCache.ContainsKey(i)).ToList();
                if (missing.Count > 0 && generation == virtualGeneration)
                {
                    var skip = missing[0];
                    var take = Math.Max(missing[^1] - skip + 1, engine.PageSize);
                    var page = await engine.Source!.GetPageAsync(
                        engine.BuildRequest(PageRequest.Offset(skip, take)), request.CancellationToken);
                    if (generation == virtualGeneration)
                    {
                        for (var i = 0; i < page.Items.Count; i++)
                        {
                            virtualCache[skip + i] = page.Items[i];
                        }

                        virtualTotal = page.TotalCount ?? virtualTotal;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (request.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DataView failed to load more rows.");
            ShowNotice($"{Texts.LoadFailed} {ex.Message}", TablerColor.Danger);
        }
        finally
        {
            virtualGate.Release();
        }

        var count = sequential
            ? virtualCache.Count + (virtualExhausted ? 0 : engine.PageSize)
            : virtualTotal ?? virtualCache.Count;
        if (sequential)
        {
            // More rows were loaded: update the row count in the footer.
            StateHasChanged();
        }
        var items = new List<TItem>(Math.Max(0, Math.Min(end, count) - start));
        for (var i = start; i < end && virtualCache.TryGetValue(i, out var item); i++)
        {
            items.Add(item);
        }

        return new ItemsProviderResult<TItem>(items, count);
    }

    private async Task FetchNextVirtualPageAsync(CancellationToken cancellationToken)
    {
        var page = engine.PagingMode == PagingMode.Cursor
            ? PageRequest.Cursor(virtualNextCursor, engine.PageSize)
            : PageRequest.Offset(virtualCache.Count, engine.PageSize);
        var result = await engine.Source!.GetPageAsync(engine.BuildRequest(page), cancellationToken);
        var offset = virtualCache.Count;
        for (var i = 0; i < result.Items.Count; i++)
        {
            virtualCache[offset + i] = result.Items[i];
        }

        virtualNextCursor = result.NextCursor;
        virtualExhausted = engine.PagingMode == PagingMode.Cursor
            ? result.NextCursor == null
            : result.Items.Count < engine.PageSize;
        if (result.TotalCount is { } total && engine.PagingMode == PagingMode.Offset)
        {
            virtualTotal = total;
        }
    }
}
