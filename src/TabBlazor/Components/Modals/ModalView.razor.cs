using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TabBlazor.Services;

namespace TabBlazor.Components.Modals
{
    public partial class ModalView : ComponentBase
    {
        [Inject] protected IModalService ModalService { get; set; }
        [Inject] protected TablerService TablerService { get; set; }

        [Parameter] public ModalModel ModalModel { get; set; }
        public string HeaderCssClass { get; private set; }
        private ElementReference BlurContainer;
        protected ElementReference dragContainer { get; set; }
        protected ElementReference contentContainer { get; set; }
        private string topOffset = "0";
        private bool isDragged;
        private bool isInitialized;

        private double startX, startY, offsetX, offsetY;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if(!isInitialized)
            {
                await BlurContainer.FocusAsync();
                if (ModalModel.Options.Draggable)
                {
                    await TablerService.SetElementProperty(dragContainer, "draggable", true);
                    await TablerService.DisableDraggable(dragContainer, contentContainer);
                }
                isInitialized = true;
            }
        }

        private string GetModalStyle()
        {
            return isDragged ? $"position:absolute; top: {offsetY}px; left: {offsetX}px;" : "";
        }

        private void OnDragStart(DragEventArgs args)
        {
            if (!ModalModel.Options.Draggable) { return; }

            if (!isDragged)
            {
                offsetX = args.ClientX - args.OffsetX;
                offsetY = args.ClientY - args.OffsetY;
            }
            startX = args.ClientX;
            startY = args.ClientY;
        }

        private void OnDragEnd(DragEventArgs args)
        {
            if (!ModalModel.Options.Draggable) { return; }

            isDragged = true;
            offsetX += args.ClientX - startX;
            offsetY += args.ClientY - startY;
        }

        //protected async Task PreventDraggable()
        //{
        //   await TablerService.SetElementProperty(dragContainer, "draggable", false);
        //}
        //protected async Task SetDraggable()
        //{
        //    await TablerService.SetElementProperty(dragContainer, "draggable", true);
        //}

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

        protected override void OnInitialized()
        {
            var modalCount = ModalService.Modals.Count();
            if (modalCount > 1 && ModalModel.Options.VerticalPosition == ModalVerticalPosition.Default)
            {
                topOffset = (20 * (modalCount - 1)).ToString();
            }

            base.OnInitialized();
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
