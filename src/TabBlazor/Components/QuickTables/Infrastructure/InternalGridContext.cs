namespace TabBlazor.Components.QuickTables.Infrastructure;

internal class InternalGridContext<TGridItem>
{
    public InternalGridContext(QuickTable<TGridItem> grid)
    {
        Grid = grid;
    }

    public QuickTable<TGridItem> Grid { get; }
    public EventCallbackSubscribable<object> ColumnsFirstCollected { get; } = new();
}