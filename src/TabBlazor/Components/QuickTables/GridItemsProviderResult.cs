namespace TabBlazor.Components.QuickTables;

public struct GridItemsProviderResult<TGridItem>
{
    public ICollection<TGridItem> Items { get; set; }

    public int TotalItemCount { get; set; }

    public GridItemsProviderResult(ICollection<TGridItem> items, int totalItemCount)
    {
        Items = items;
        TotalItemCount = totalItemCount;
    }
}

public static class GridItemsProviderResult
{
    public static GridItemsProviderResult<TGridItem> From<TGridItem>(ICollection<TGridItem> items, int totalItemCount)
    {
        return new GridItemsProviderResult<TGridItem>(items, totalItemCount);
    }
}