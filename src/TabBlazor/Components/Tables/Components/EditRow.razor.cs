using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using TabBlazor.Services;

namespace TabBlazor.Components.Tables
{
    public class EditRowBase<TableItem> : TableRowComponentBase<TableItem>
    {
        [Inject] private TablerService tabService { get; set; }
        [Parameter] public IInlineEditTable<TableItem> InlineEditTable { get; set; }
        [Parameter] public TableItem Item { get; set; }

        protected ElementReference editRow;
        private bool isInitialized;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!isInitialized)
            {
                await tabService.FocusFirstInTableRow(editRow);
                isInitialized = true;
            }

        }

        public async Task OnEditItemCanceled()
        {
            await InlineEditTable.CancelEdit();
        }



    }
}
