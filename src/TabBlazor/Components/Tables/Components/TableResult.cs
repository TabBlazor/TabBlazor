namespace TabBlazor.Components.Tables.Components
{
    public class TableResult<TKey, TElement> : List<TElement>, IGrouping<TKey, TElement>
    {
        public bool Expanded = true;

        public TableResult(TKey key, List<TElement> elements)
        {
            Key = key;
            AddRange(elements);
        }

        public RenderFragment<TableResult<TKey, TElement>> GroupingTemplate { get; set; }

        public TKey Key { get; set; }
    }
}