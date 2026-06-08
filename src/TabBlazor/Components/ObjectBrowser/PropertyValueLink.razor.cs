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
    /// <summary>Renders a clickable value that opens an <see cref="TabBlazor.ObjectBrowser"/> on the value when clicked.</summary>
    public partial class PropertyValueLink
    {
        [Inject] public IModalService ModalService { get; set; }
        /// <summary>The value to inspect when the link is clicked.</summary>
        [Parameter] public object PropertyValue { get; set; }

        /// <summary>The link content to display.</summary>
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