#nullable enable
using System.Globalization;
using Microsoft.JSInterop;

namespace TabBlazor;

public partial class DataView<TItem>
{
    private readonly Dictionary<string, int> columnWidths = [];

    // The table element resizing is attached to; a rebuilt table (other view, side panel to drawer) gets it again.
    private string? resizeAttachedTo;

    /// <summary>Lets the user resize columns by dragging the edge of their header.</summary>
    [Parameter] public bool ResizableColumns { get; set; }

    internal ElementReference TableElement { get; set; }

    /// <summary>Column widths in pixels set by the user, by column id.</summary>
    public IReadOnlyDictionary<string, int> ColumnWidths => columnWidths;

    internal string? GetHeaderStyle(IDataViewColumn<TItem> column) =>
        columnWidths.TryGetValue(column.Id, out var width)
            ? string.Create(CultureInfo.InvariantCulture, $"width: {width}px;min-width: {width}px;")
            : column.GetHeaderStyle();

    /// <summary>Called by JS when the user has dragged a column to a new width.</summary>
    [JSInvokable]
    public Task OnColumnResized(string columnId, int width) => InvokeAsync(() =>
    {
        columnWidths[columnId] = Math.Max(40, width);
        StateHasChanged();
    });

    private async Task AttachColumnResizeAsync()
    {
        if (!ResizableColumns || CurrentView != DataViewKind.Table || TableElement.Context == null ||
            TableElement.Id == resizeAttachedTo)
        {
            return;
        }

        try
        {
            selfReference ??= DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("tabBlazor.attachColumnResize", TableElement, selfReference);
            resizeAttachedTo = TableElement.Id;
        }
        catch (JSDisconnectedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}
