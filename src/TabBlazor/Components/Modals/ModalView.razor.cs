using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;
using TabBlazor.Services;

namespace TabBlazor
{
    public partial class ModalView : ComponentBase, IDisposable
    {

        [Inject] protected TablerService TablerService { get; set; }
        [Inject] private IModalService ModalService { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public ModalOptions Options { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public EventCallback OnClosed { get; set; }

        public string HeaderCssClass { get; private set; }
        private ElementReference BlurContainer;
        protected ElementReference dragContainer { get; set; }
        protected ElementReference contentContainer { get; set; }

        private bool isDragged;
        private bool isInitialized;

        private double startX, startY, offsetX, offsetY;
        private ModalViewSettings modalViewSettings;

        protected override void OnInitialized()
        {
            modalViewSettings = ModalService.RegisterModalView(this);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!isInitialized)
            {

                await BlurContainer.FocusAsync();
                if (Options.Draggable)
                {
                    await TablerService.SetElementProperty(dragContainer, "draggable", true);
                    await TablerService.DisableDraggable(dragContainer, contentContainer);
                }
                isInitialized = true;
            }
        }

        public void Close()
        {
            OnClosed.InvokeAsync();
        }

        private string GetModalStyle()
        {
            return isDragged ? $"position:absolute; top: {offsetY}px; left: {offsetX}px;" : $"top: {modalViewSettings.TopOffset}px";
        }

        private void OnDragStart(DragEventArgs args)
        {
            if (!Options.Draggable) { return; }

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
            if (!Options.Draggable) { return; }

            isDragged = true;
            offsetX += args.ClientX - startX;
            offsetY += args.ClientY - startY;
        }

        protected void OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Escape" && Options.CloseOnEsc)
            {
                Close();
            }
        }

        protected void OnClickOutside(MouseEventArgs e)
        {
            if (Options.CloseOnClickOutside)
            {
                Close();
            }
        }

        protected string GetModalCss() => new ClassBuilder()
                .Add("modal-dialog")
                .AddIf("modal-sm", Options.Size == ModalSize.Small)
                .AddIf("modal-lg", Options.Size == ModalSize.Large)
                .AddIf("modal-xl", Options.Size == ModalSize.XLarge)
                .AddIf("modal-max", Options.Size == ModalSize.Maximized)
                .AddIf("modal-fullscreen", Options.Fullscreen == ModalFullscreen.Allways)
                .AddIf("modal-fullscreen-sm-down", Options.Fullscreen == ModalFullscreen.BelowSmall)
                .AddIf("modal-fullscreen-md-down", Options.Fullscreen == ModalFullscreen.BelowMedium)
                .AddIf("modal-fullscreen-lg-down", Options.Fullscreen == ModalFullscreen.BelowLarge)
                .AddIf("modal-fullscreen-xl-down", Options.Fullscreen == ModalFullscreen.BelowXLarge)
                .AddIf("modal-fullscreen-xxl-down", Options.Fullscreen == ModalFullscreen.BelowXXLarge)
                .AddIf("modal-dialog-scrollable", Options.Scrollable)
                .AddIf("modal-dialog-centered", Options.VerticalPosition == ModalVerticalPosition.Centered)
                .ToString();

        public void Dispose()
        {
            ModalService.UnRegisterModalView(this);
        }
    }
}
