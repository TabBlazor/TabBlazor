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
               .AddIf("table-primary", IsSelected(item) && (Table.OnItemSelected.HasDelegate || Table.SelectedItemChanged.HasDelegate))
               .ToString();
        }

        protected bool IsSelected(TableItem item)
        {
            return EqualityComparer<TableItem>.Default.Equals(item, Table.SelectedItem);
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