using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using TabBlazor.Services;

namespace TabBlazor.Components.ObjectBrowser
{
    public partial class PropertyValueLink
    {
        [Inject] public IModalService ModalService { get; set; }
        [Parameter] public object PropertyValue { get; set; }

        [Parameter] public RenderFragment ChildContent { get; set; }

        private async Task ObjectDetails()
        {
            if(PropertyValue != null)
            {
                var component = new RenderComponent<TabBlazor.ObjectBrowser>().Set(e => e.Object, PropertyValue);
                var result = await ModalService.ShowAsync(PropertyValue.GetType().FullName, component, new ModalOptions { Size = ModalSize.XLarge });
            }
        
        }

    }
}