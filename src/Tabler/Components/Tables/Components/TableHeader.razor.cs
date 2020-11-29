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
                .AddIf("sorting", !column.SortColumn && column.Sortable)
                .AddIf("sorting_desc", column.SortColumn && column.SortDescending)
                .AddIf("sorting_asc", column.SortColumn && !column.SortDescending)
                .ToString();
            //return new CssBuilder()
            //    .AddClass("")
            //    .AddClass("sortable", column.Sortable)
            //    .AddClass("sorting_desc", column.SortColumn && column.SortDescending)
            //    .AddClass("sorting_asc", column.SortColumn && !column.SortDescending)
            //    .AddClass("sorting", !column.SortColumn && column.Sortable)
            //    .AddClass("display-none", !column.Visible)
            //    .Build();
        }
    }
}