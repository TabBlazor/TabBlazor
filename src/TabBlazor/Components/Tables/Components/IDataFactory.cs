namespace TabBlazor.Components.Tables.Components
{
    public interface IDataProvider<Item>
    {
        public Task<IEnumerable<TableResult<object, Item>>> GetData(List<IColumn<Item>> columns, ITableState<Item> state, IEnumerable<Item> items, bool resetPage = false, bool addSorting = true, Item moveToItem = default);
    }
}
