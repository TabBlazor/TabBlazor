using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TabBlazor.Components.Tables;
using TabBlazor.Services;

namespace TabBlazor.Components.Tables
{
    public class TableRowBase<TableItem> : TableRowComponentBase<TableItem>
    {
        [Inject] private TablerService tabService { get; set; }

        [Parameter] public ITableRow<TableItem> Table { get; set; }
        [Parameter] public TableItem Item { get; set; }
        [Parameter] public ITableRowActions<TableItem> Actions { get; set; }

        protected ElementReference[] tableCells;

        protected override void OnInitialized()
        {
            tableCells = new ElementReference[Table.VisibleColumns.Count + 2];
        }

        protected int GetTabIndex()
        {
            return Table.KeyboardNavigation ? 0 : -1;
        }

        protected bool CanDelete()
        {
            if (!Table.AllowDelete)
            {
                return false;
            }

            if (Table.AllowDeleteExpression == null)
            {
                return true;
            }

            return Table.AllowDeleteExpression(Item);
        }

        protected bool CanEdit()
        {
            if (!Table.AllowEdit)
            {
                return false;
            }

            if (Table.AllowEditExpression == null)
            {
                return true;
            }

            return Table.AllowEditExpression(Item);
        }


        public string GetRowCssClass(TableItem item)
        {
            return new ClassBuilder()
               .Add("data-row")
               .AddIf("table-active", IsSelected(item) && (Table.OnItemSelected.HasDelegate || Table.SelectedItemsChanged.HasDelegate))
               .ToString();
        }

        protected async Task OnKeyDown(KeyboardEventArgs e, ElementReference tableCell)
        {
            if (e.Key == "ArrowUp" || e.Key == "ArrowDown")
            {
                await tabService.NavigateTable(tableCell, e.Key);
            }
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