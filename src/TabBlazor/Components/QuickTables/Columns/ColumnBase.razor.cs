using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using TabBlazor.Components.QuickTables.Infrastructure;

namespace TabBlazor.Components.QuickTables;

public abstract partial class ColumnBase<TGridItem>
{
    [CascadingParameter]
    internal InternalGridContext<TGridItem> InternalGridContext { get; set; } = default!;

    /// <summary>
    /// The column header text.
    /// </summary>
    [Parameter]
    public string Title { get; set; }

    /// <summary>
    /// Additional CSS class applied to the column's cells.
    /// </summary>
    [Parameter]
    public string Class { get; set; }

    /// <summary>
    /// Horizontal alignment of the column's content.
    /// </summary>
    [Parameter]
    public Align Align { get; set; }

    /// <summary>
    /// Custom header content. When set, replaces the default header rendering.
    /// </summary>
    [Parameter]
    public RenderFragment<ColumnBase<TGridItem>> HeaderTemplate { get; set; }

    /// <summary>
    /// Content shown in the column options popup, surfaced via the header options button.
    /// </summary>
    [Parameter]
    public RenderFragment ColumnOptions { get; set; }

    /// <summary>
    /// Whether the column is sortable. When <c>null</c>, falls back to the column type's default.
    /// </summary>
    [Parameter]
    public bool? Sortable { get; set; }

    /// <summary>
    /// When set, marks this column as the default sort and specifies its direction.
    /// </summary>
    [Parameter]
    public SortDirection IsDefaultSort { get; set; }

    /// <summary>
    /// Template rendered in each cell while data is loading.
    /// </summary>
    [Parameter]
    public RenderFragment<PlaceholderContext> PlaceholderTemplate { get; set; }

    /// <summary>
    /// The owning <see cref="QuickTable{TGridItem}"/> instance.
    /// </summary>
    public QuickTable<TGridItem> Grid => InternalGridContext.Grid;

    protected internal abstract void CellContent(RenderTreeBuilder builder, TGridItem item);

    protected internal RenderFragment HeaderContent { get; protected set; }

    protected virtual bool IsSortableByDefault()
    {
        return false;
    }

    public ColumnBase()
    {
        HeaderContent = RenderDefaultHeaderContent;
    }
}
