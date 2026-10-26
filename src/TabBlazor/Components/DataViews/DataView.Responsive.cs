#nullable enable
using Microsoft.JSInterop;

namespace TabBlazor;

/// <summary>How a table looks below the mobile breakpoint.</summary>
public enum DataMobileLayout
{
    /// <summary>Each row is a card with "Label: value" lines built from the column titles.</summary>
    Stacked,
    /// <summary>Columns are kept and scroll sideways; the first column and the actions stay pinned.</summary>
    Scroll,
    /// <summary>Your own card markup per row, from <c>MobileTemplate</c>.</summary>
    Template
}

public partial class DataView<TItem>
{
    private readonly string mediaWatchId = $"dataview-{Guid.CreateVersion7():N}";
    private DotNetObjectReference<DataView<TItem>>? selfReference;
    private string? watchedQuery;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>How the table looks below <see cref="MobileBreakpoint"/>. Defaults to stacked cards (a <c>MobileTemplate</c> switches to your own markup; a virtualized table scrolls instead of stacking).</summary>
    [Parameter] public DataMobileLayout MobileLayout { get; set; } = DataMobileLayout.Stacked;

    /// <summary>The Bootstrap breakpoint below which the small-screen layout applies: sm, md (default), lg, xl or xxl.</summary>
    [Parameter] public string MobileBreakpoint { get; set; } = "md";

    internal RenderFragment<TItem>? MobileTemplate { get; private set; }

    /// <summary>True when the viewport is below <see cref="MobileBreakpoint"/> (known after the first interactive render).</summary>
    public bool IsSmallScreen { get; private set; }

    // Stacked cards are taller than ItemSize, so virtualized tables keep their rows and scroll sideways.
    internal DataMobileLayout EffectiveMobileLayout => MobileTemplate != null ? DataMobileLayout.Template
        : MobileLayout == DataMobileLayout.Stacked && IsVirtualized ? DataMobileLayout.Scroll
        : MobileLayout;

    internal string Breakpoint => NormalizeBreakpoint(MobileBreakpoint);

    /// <summary>Where details appear: the side panel and inline details fall back to the drawer where they cannot work.</summary>
    internal DataDetailsMode EffectiveDetailsMode => DetailsMode switch
    {
        DataDetailsMode.SidePanel when IsSmallScreen => DataDetailsMode.Drawer,
        DataDetailsMode.Inline when IsVirtualized => DataDetailsMode.Drawer,
        _ => DetailsMode
    };

    internal OffcanvasSize DrawerSize(OffcanvasSize normal) => IsSmallScreen ? OffcanvasSize.Full : normal;

    internal static string NormalizeBreakpoint(string? breakpoint) => breakpoint?.Trim().ToLowerInvariant() switch
    {
        "sm" => "sm",
        "md" or null or "" => "md",
        "lg" => "lg",
        "xl" => "xl",
        "xxl" => "xxl",
        _ => throw new ArgumentException($"Unknown breakpoint '{breakpoint}'. Use sm, md, lg, xl or xxl.")
    };

    private static int BreakpointWidth(string breakpoint) => breakpoint switch
    {
        "sm" => 576,
        "lg" => 992,
        "xl" => 1200,
        "xxl" => 1400,
        _ => 768
    };

    private void SetResponsiveSlot(DataViewSlot slot, object? template)
    {
        if (slot == DataViewSlot.MobileTemplate)
        {
            MobileTemplate = (RenderFragment<TItem>?)template;
        }
    }

    private async Task WatchScreenSizeAsync()
    {
        var query = $"(max-width: {BreakpointWidth(Breakpoint) - 0.02:0.00}px)".Replace(',', '.');
        if (query == watchedQuery || disposed)
        {
            return;
        }

        try
        {
            if (watchedQuery != null)
            {
                await JS.InvokeVoidAsync("tabBlazor.unwatchMedia", mediaWatchId);
            }

            selfReference ??= DotNetObjectReference.Create(this);
            watchedQuery = query;
            var small = await JS.InvokeAsync<bool>("tabBlazor.watchMedia", mediaWatchId, query, selfReference);
            if (small != IsSmallScreen)
            {
                await OnMediaChanged(small);
            }
        }
        catch (JSDisconnectedException)
        {
        }
        catch (InvalidOperationException)
        {
            // Prerendering: JS is not available yet.
        }
    }

    /// <summary>Called by JS when the viewport crosses the mobile breakpoint.</summary>
    [JSInvokable]
    public Task OnMediaChanged(bool matches) => InvokeAsync(() =>
    {
        IsSmallScreen = matches;
        engine.InvalidateRows();
        StateHasChanged();
    });

    private async ValueTask DisposeResponsiveAsync()
    {
        if (watchedQuery != null)
        {
            try
            {
                await JS.InvokeVoidAsync("tabBlazor.unwatchMedia", mediaWatchId);
            }
            catch (JSDisconnectedException)
            {
            }
            catch (JSException)
            {
            }
            catch (TaskCanceledException)
            {
            }
        }

        selfReference?.Dispose();
    }
}
