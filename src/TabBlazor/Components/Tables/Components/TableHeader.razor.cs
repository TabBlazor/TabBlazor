namespace TabBlazor.Components.Tables
{
    public class TableHeaderBase<TableItem> : TableRowComponentBase<TableItem>
    {
        [CascadingParameter(Name = "Table")] public ITable<TableItem> Table { get; set; }

        public string GetColumnHeaderClass(IColumn<TableItem> column)
        {
            return new ClassBuilder()
                .AddIf("cursor-pointer", column.Sortable)
                .AddIf("text-end", column.Align == Align.End)
                .ToString();
        }

        protected string GetSortIconClass(IColumn<TableItem> column)
        {
            if (!column.SortColumn && column.Sortable)
            {
                return "sorting";
            }

            if (column.SortColumn && column.SortDescending)
            {
                return "sorting_desc";
            }

            if (column.SortColumn && !column.SortDescending)
            {
                return "sorting_desc";
            }

            return string.Empty;
        }

        protected IIconType GetSortIcon(IColumn<TableItem> column)
        {
            if (!column.SortColumn && column.Sortable)
            {
                return InternalIcons.Sortable;
            }

            if (column.SortColumn && column.SortDescending)
            {
                return InternalIcons.Sort_Desc;
            }

            if (column.SortColumn && !column.SortDescending)
            {
                return InternalIcons.Sort_Asc;
            }

            return null;
        }

        protected bool? SelectedValue()
        {
            if (Table.SelectedItems == null || !Table.SelectedItems.Any())
            {
                return false;
            }

            if (Table.SelectAllStrategy == SelectAllStrategy.AllPages && Table.SelectedItems.Count == Table.TotalCount)
            {
                return true;
            }

            if (Table.SelectAllStrategy != SelectAllStrategy.AllPages && Table.SelectedItems.Count == Table.CurrentItems.Count)
            {
                return true;
            }

            return null;
        }

        protected void ToggleSelected(bool? value)
        {
            var selected = SelectedValue();
            if (selected != true)
            {
                Table.SelectAll();
            }
            else
            {
                Table.UnSelectAll();
            }
        }
    }
}