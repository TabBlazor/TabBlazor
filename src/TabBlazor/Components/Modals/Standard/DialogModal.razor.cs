using Microsoft.AspNetCore.Components;
using TabBlazor.Services;

namespace TabBlazor.Components.Modals
{
    /// <summary>
    /// The built-in confirm/alert dialog shown by <see cref="Services.IModalService.ShowDialogAsync"/>.
    /// </summary>
    public partial class DialogModal : ComponentBase
    {
        [Inject] private IModalService modalService { get; set; }

        /// <summary>The dialog content and button configuration.</summary>
        [Parameter] public DialogOptions Options { get; set; }
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
