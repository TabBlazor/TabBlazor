using Microsoft.AspNetCore.Components;
using Tabler.Components.Tables;

namespace Tabler.Components.Tables
{
    public class TableHeaderBase<TableItem> : TableRowComponentBase<TableItem>
    {
        [CascadingParameter(Name = "Table")] public ITable<TableItem> Table { get; set; }

        public string GetColumnHeaderClass(IColumn<TableItem> column)
        {
            return new ClassBuilder()
                .AddIf("cursor-pointer", column.Sortable)
                .AddIf("sorting", !column.SortColumn && column.Sortable)
                .AddIf("sorting_asc", column.SortColumn && column.SortDescending)
                .AddIf("sorting_desc", column.SortColumn && !column.SortDescending)
                .ToString();
         }
    }
}