using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using TabBlazor.Services;

namespace TabBlazor.Components.Tables.Components
{
    public partial class PopupEdit<TItem>
    {
        [Inject] private IModalService modalService { get; set; }
        [Parameter] public IPopupEditTable<TItem> Table { get; set; }
     
     
        private async Task CancelEdit()
        {
            await Table.CancelEdit();
            modalService.Close();
        }

        private async Task CloseEdit()
        {
          //  await Table.CloseEdit();
            modalService.Close();
        }

    }
}