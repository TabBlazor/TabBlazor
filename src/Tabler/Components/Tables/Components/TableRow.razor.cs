using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;
using TabBlazor.Components.Tables;

namespace TabBlazor.Components.Tables
{
    public class TableRowBase<TableItem> : TableRowComponentBase<TableItem>
    {
        [Parameter] public ITableRow<TableItem> Table { get; set; }
        [Parameter] public TableItem Item { get; set; }
        [Parameter] public ITableRowActions<TableItem> Actions { get; set; }

        public string GetRowCssClass(TableItem item)
        {
            return "";
            //return new CssBuilder()
            //    .AddClass("")
            //    .AddClass("selected-details", IsSame(Table.SelectedItem, item))
            //    .AddClass("hover-row", Table.DetailsTemplate != null || Table.OnItemSelected.HasDelegate || !Table.OnItemSelected.Equals(default(EventCallback<TableItem>)))
            //    .Build();
        }

        public bool IsSame(TableItem first, TableItem second)
        {
            return EqualityComparer<TableItem>.Default.Equals(first, second);
        }

        protected bool ShowRowAction => Table.RowActionTemplate != null || Table.AllowDelete || Table.AllowEdit;

        protected async Task Delete()
        {
            await Table.OnDeleteItem(Item);
        }

        protected void Edit()
        {
            Table.EditItem(Item);
        }

    }
}