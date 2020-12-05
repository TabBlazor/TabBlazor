using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using TabBlazor.Components.Tables;

namespace TabBlazor.Components.Tables
{
    public class TableRowActionsBase<TableItem> : TableRowComponentBase<TableItem>
    {
        [Parameter] public ITableRowActions<TableItem> Table { get; set; }
        [Parameter] public TableItem Item { get; set; }

       
        protected bool CanDeleteItem(TableItem item)
        {
            if (!Table.AllowDelete)
            {
                return false;
            }

            if (Table.AllowDeleteExpression != null)
            {
                return Table.AllowDeleteExpression.Invoke(item);
            }

            return true;
        }

        //protected async Task ActionClicked(MenuDropdownItem<TableItem> menuItem, TableItem item)
        //{
        //    if (menuItem.Href != null)
        //    {
        //        var href = menuItem.Href.Invoke(item);
        //        Navigation.NavigateTo(href);
        //    }
        //    else
        //    {
        //        await menuItem.CallBack.InvokeAsync(item);
        //    }
        //}
    }
}