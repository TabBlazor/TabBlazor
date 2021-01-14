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
                .AddIf("sorting", !column.SortColumn && column.Sortable)
                .AddIf("sorting_desc", column.SortColumn && column.SortDescending)
                .AddIf("sorting_asc", column.SortColumn && !column.SortDescending)
                .ToString();
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