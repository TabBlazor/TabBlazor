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
            return new ClassBuilder()
               .AddIf("table-active", IsSelected(item) && (Table.OnItemSelected.HasDelegate || Table.SelectedItemsChanged.HasDelegate))
               .ToString();
        }

        public async Task RowClick()
        {
            await Table.RowClicked(Item);
        }

        public bool IsSelected(TableItem item)
        {
            if (Table.SelectedItems == null) { return false; }
            return Table.SelectedItems.Contains(item);
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