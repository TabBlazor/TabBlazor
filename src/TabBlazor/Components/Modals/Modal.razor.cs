using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TabBlazor.Services;

namespace TabBlazor.Components
{
    public partial class Modal : ComponentBase
    {
      
        [Inject] protected IModalService ModalService { get; set; }

        protected bool IsVisible { get; set; }
        protected string Title { get; set; }
        protected RenderFragment Content { get; set; }
        protected ModalParameters Parameters { get; set; }
        public string HeaderCssClass { get; private set; }

        protected override void OnInitialized()
        {
            ((ModalService)ModalService).OnShow += ShowModal;
            ((ModalService)ModalService).OnTitleSet += SetTitle;
            ModalService.OnClose += CloseModal;
        }

        public void SetTitle(string title)
        {
            Title = title;
            InvokeAsync(StateHasChanged);
        }

        public void ShowModal(string title, string headerCssClass, RenderFragment content, ModalParameters parameters)
        {
            Title = title;
            Content = content;
            Parameters = parameters;
            HeaderCssClass = headerCssClass;
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
