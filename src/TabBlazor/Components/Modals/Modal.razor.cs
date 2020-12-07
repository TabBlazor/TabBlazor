using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TabBlazor.Services;

namespace TabBlazor
{
    public partial class Modal : ComponentBase
    {

        [Inject] protected IModalService ModalService { get; set; }

        protected bool IsVisible { get; set; }
        protected string Title { get; set; }
        protected RenderFragment Content { get; set; }
        protected ModalParameters Parameters { get; set; }
        public string HeaderCssClass { get; private set; }

        protected ModalOptions modalOptions = new ModalOptions();

        protected override void OnInitialized()
        {
            ((ModalService)ModalService).OnShow += ShowModal;
            ((ModalService)ModalService).OnTitleSet += SetTitle;
            ModalService.OnClose += CloseModal;
        }

        protected string GetModalCss() => new ClassBuilder()
                .Add("modal-dialog")
                .AddIf("modal-xs", modalOptions.Size == ModalSize.XSmall)
                .AddIf("modal-sm", modalOptions.Size == ModalSize.Small)
                .AddIf("modal-lg", modalOptions.Size == ModalSize.Large)
                .AddIf("modal-xl", modalOptions.Size == ModalSize.XLarge)
                .AddIf("modal-fullscreen", modalOptions.Fullscreen == ModalFullscreen.Allways)
                .AddIf("modal-fullscreen-sm-down", modalOptions.Fullscreen == ModalFullscreen.BelowSmall)
                .AddIf("modal-fullscreen-md-down", modalOptions.Fullscreen == ModalFullscreen.BelowMedium)
                .AddIf("modal-fullscreen-lg-down", modalOptions.Fullscreen == ModalFullscreen.BelowLarge)
                .AddIf("modal-fullscreen-xl-down", modalOptions.Fullscreen == ModalFullscreen.BelowXLarge)
                .AddIf("modal-fullscreen-xxl-down", modalOptions.Fullscreen == ModalFullscreen.BelowXXLarge)
                .ToString();

        public void SetTitle(string title)
        {
            Title = title;
            InvokeAsync(StateHasChanged);
        }

        public void ShowModal(string title, ModalOptions modalOptions, RenderFragment content, ModalParameters parameters)
        {
            Title = title;
            Content = content;
            Parameters = parameters;
            this.modalOptions = modalOptions;
            IsVisible = true;
            InvokeAsync(StateHasChanged);
        }

        internal void CloseModal(ModalResult modalResult)
        {
            IsVisible = false;
            Title = "";
            Content = null;
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            ((ModalService)ModalService).OnShow -= ShowModal;
            ModalService.OnClose -= CloseModal;
        }


    }
}
