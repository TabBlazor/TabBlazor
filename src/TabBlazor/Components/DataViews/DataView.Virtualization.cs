#nullable enable
namespace TabBlazor;

public partial class DataView<TItem>
{
    /// <summary>
    /// Shows one scrolling list instead of pages, rendering only the visible rows. Offset sources with a total get
    /// true virtualization; cursor sources scroll infinitely. Rows need a fixed height (<see cref="ItemSize"/>) and the
    /// view a <see cref="Height"/>.
    /// </summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>The fixed row height in pixels while virtualized. Defaults to 44.</summary>
    [Parameter] public float ItemSize { get; set; } = 44;

    /// <summary>How many extra rows render above and below the visible ones while virtualized. Defaults to 3.</summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    internal bool IsVirtualized => Virtualize;
}
