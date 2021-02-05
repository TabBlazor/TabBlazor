using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TabBlazor.Services;

namespace TabBlazor.Components.Modals
{
    public partial class ModalView : ComponentBase
    {
        [Inject] protected IModalService ModalService { get; set; }

        [Parameter] public ModalModel ModalModel { get; set; }
        public string HeaderCssClass { get; private set; }

        protected ElementReference BlurContainer;

        protected void OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Escape" && ModalModel.Options.CloseOnEsc)
            {
                ModalService.Close();
            }
        }

        protected void OnClickOutside(MouseEventArgs e)
        {
            if (ModalModel.Options.CloseOnClickOutside)
            {
                ModalService.Close();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                await BlurContainer.FocusAsync();
            }
        }

        protected string GetModalCss() => new ClassBuilder()
                .Add("modal-dialog")
                .AddIf("modal-sm", ModalModel.Options.Size == ModalSize.Small)
                .AddIf("modal-lg", ModalModel.Options.Size == ModalSize.Large)
                .AddIf("modal-xl", ModalModel.Options.Size == ModalSize.XLarge)
                .AddIf("modal-max", ModalModel.Options.Size == ModalSize.Maximized)
                .AddIf("modal-fullscreen", ModalModel.Options.Fullscreen == ModalFullscreen.Allways)
                .AddIf("modal-fullscreen-sm-down", ModalModel.Options.Fullscreen == ModalFullscreen.BelowSmall)
                .AddIf("modal-fullscreen-md-down", ModalModel.Options.Fullscreen == ModalFullscreen.BelowMedium)
                .AddIf("modal-fullscreen-lg-down", ModalModel.Options.Fullscreen == ModalFullscreen.BelowLarge)
                .AddIf("modal-fullscreen-xl-down", ModalModel.Options.Fullscreen == ModalFullscreen.BelowXLarge)
                .AddIf("modal-fullscreen-xxl-down", ModalModel.Options.Fullscreen == ModalFullscreen.BelowXXLarge)
                .AddIf("modal-dialog-scrollable", ModalModel.Options.Scrollable)
                .AddIf("modal-dialog-centered", ModalModel.Options.VerticalPosition == ModalVerticalPosition.Centered)
                .ToString();





    }
}
