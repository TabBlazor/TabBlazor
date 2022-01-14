using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TabBlazor;
using TabBlazor.Services;

namespace Tabler.Docs.Components.TypeBrowsers
{
    public partial class TypeBrowser : ComponentBase
    {
        [Inject] private IModalService modalService { get; set; }
        [Parameter] public Type Type { get; set; }

        private IList<PropertyView> properties;
        private List<MethodInfo> methods;

        protected override void OnInitialized()
        {
            if (Type == null)
            {
                return;
            }

            modalService.UpdateTitle(Type.GetFriendlyName());

            properties = Type.GetProperties().Select(e => new PropertyView(e)).ToList();

            //methods = Type
            //           .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
            //           .Where(m => !m.IsSpecialName).ToList();

            methods = Type
                .GetMethods()
                .Where(m => !m.IsSpecialName && !m.IsVirtual && m.MethodImplementationFlags != MethodImplAttributes.InternalCall)
                .ToList();
        }


    }
}
