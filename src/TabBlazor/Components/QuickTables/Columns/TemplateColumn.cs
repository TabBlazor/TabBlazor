namespace TabBlazor.Components.QuickTables;

public class TemplateColumn<TGridItem> : ColumnBase<TGridItem>, ISortBuilderColumn<TGridItem>
{
    private static readonly RenderFragment<TGridItem> EmptyChildContent = _ => builder => { };

    [Parameter] public RenderFragment<TGridItem> ChildContent { get; set; } = EmptyChildContent;

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