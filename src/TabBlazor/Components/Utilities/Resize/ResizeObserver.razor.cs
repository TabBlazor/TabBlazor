using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using TabBlazor.Services;
using Microsoft.JSInterop;

namespace TabBlazor
{
    public partial class ResizeObserver
    {
        [Inject] private IJSRuntime jSRuntime { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public EventCallback<ResizeObserverEntry> OnResized { get; set; }

        private ElementReference elementRef;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await jSRuntime.InvokeVoidAsync("tabBlazor.addResizeObserver", elementRef, DotNetObjectReference.Create(this));
         }
        }

        [JSInvokable]
        public async Task ElementResized(ResizeObserverEntry resizeObserverEntry)
        {
            await OnResized.InvokeAsync(resizeObserverEntry);
        }
       

    }
}