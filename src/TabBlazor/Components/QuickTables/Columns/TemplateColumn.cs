namespace TabBlazor.Components.QuickTables;

/// <summary>
/// A <see cref="QuickTable{TGridItem}"/> column that renders arbitrary content per row via a template, rather
/// than a single bound property. Sortable only when <see cref="SortBy"/> is supplied.
/// </summary>
public class TemplateColumn<TGridItem> : ColumnBase<TGridItem>, ISortBuilderColumn<TGridItem>
{
    private static readonly RenderFragment<TGridItem> EmptyChildContent = _ => builder => { };

    /// <summary>The template rendered for each row, receiving the row item as context.</summary>
    [Parameter] public RenderFragment<TGridItem> ChildContent { get; set; } = EmptyChildContent;

    /// <summary>Optional sort definition; when set, the column becomes sortable.</summary>
    [Parameter] public GridSort<TGridItem> SortBy { get; set; }

    GridSort<TGridItem> ISortBuilderColumn<TGridItem>.SortBuilder => SortBy;

    protected internal override void CellContent(RenderTreeBuilder builder, TGridItem item)
    {
        builder.AddContent(0, ChildContent(item));
    }

    protected override bool IsSortableByDefault()
    {
        return SortBy is not null;
    }
}