using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace TabBlazor.Components.Tables
{
    public class EditRowBase<TableItem> : TableRowComponentBase<TableItem>
    {
        [Parameter] public IInlineEditTable<TableItem> InlineEditTable { get; set; }
        [Parameter] public TableItem Item { get; set; }

        public async Task OnEditItemCanceled()
        {
            if (InlineEditTable.IsAddInProgress)
            {
                InlineEditTable.Items.Remove(InlineEditTable.CurrentEditItem);
            }

            await InlineEditTable.CloseEdit();
        }
    }
}