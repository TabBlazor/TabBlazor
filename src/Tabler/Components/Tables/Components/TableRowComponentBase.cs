
using Microsoft.AspNetCore.Components;
using Tabler.Components.Tables;

namespace Tabler.Components.Tables
{
    public abstract class TableRowComponentBase<TableItem> : ComponentBase
    {
        public string GetColumnWidth(IColumn<TableItem> column)
        {
            return !string.IsNullOrEmpty(column.Width) ? $"width:{column.Width}; " : "";
        }

        public string GetColumnClass(IColumn<TableItem> column)
        {
            return "";
            //return new CssBuilder()
            //    .AddClass("")
            //    .AddClass("display-none", !column.Visible)
            //    .AddClass(column.CssClass, column.CssClass != null)
            //    .Build();
        }
    }
}