using Microsoft.AspNetCore.Components;
using System.Linq;
using TabBlazor.Components.Tables;

namespace TabBlazor.Components.Tables
{
    public class TableHeaderBase<TableItem> : TableRowComponentBase<TableItem>
    {
        [CascadingParameter(Name = "Table")] public ITable<TableItem> Table { get; set; }

        public string GetColumnHeaderClass(IColumn<TableItem> column)
        {
            return new ClassBuilder()
                .AddIf("cursor-pointer", column.Sortable)
                .ToString();
         }


        protected string GetSortIconClass(IColumn<TableItem> column)
        {
            if (!column.SortColumn && column.Sortable) { return "sorting"; }
            if (column.SortColumn && column.SortDescending) { return "sorting_desc"; }
            if (column.SortColumn && !column.SortDescending) { return "sorting_desc"; }
            return string.Empty;
        }

        protected IIconType GetSortIcon(IColumn<TableItem> column)
        {
            if (!column.SortColumn && column.Sortable) { return InternalIcons.Sortable; }
            if (column.SortColumn && column.SortDescending) { return InternalIcons.Sort_Desc; }
            if (column.SortColumn && !column.SortDescending) { return InternalIcons.Sort_Asc; }

            return null;
        }

        protected bool? SelectedValue()
        {
            if (Table.SelectedItems == null || !Table.SelectedItems.Any()) { return false; }
            if (Table.SelectedItems.Count == Table.Items.Count) { return true; }
            if (Table.SelectedItems.Any()) { return null; }
            return true;
        }

        protected void ToogleSelected()
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
