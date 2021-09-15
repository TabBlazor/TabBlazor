using Microsoft.AspNetCore.Components;
using TabBlazor.Services;

namespace TabBlazor.Components.Modals.Standard
{
    public partial class ConfirmModal : ComponentBase
    {
        [Inject] private IModalService modalService { get; set; }

        [Parameter] public ConfirmOptions Options { get; set; }
        private void Cancel()
        {
            modalService.Close();
        }

        private void Ok()
        {
            modalService.Close(ModalResult.Ok());
        }



    }
}
