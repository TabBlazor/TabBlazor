using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TabBlazor;
using TabBlazor.Services;
using Tabler.Docs.Components.TypeBrowsers;

namespace Tabler.Docs.Components
{
   public partial class DocsExample : ComponentBase
    {
        [Inject] public TablerService TablerService { get; set; }
        [Inject] private IModalService modalService { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public RenderFragment Description { get; set; }
        [Parameter] public Type ComponentType { get; set; }

        private async Task NavigateTo(CodeSnippet codeSnippet)
        {
            await TablerService.ScrollToFragment(codeSnippet.Id.ToString());
        }

        public List<CodeSnippet> CodeSnippets = new List<CodeSnippet>();
        public void AddCodeSnippet(CodeSnippet codeSnippet)
        {
            CodeSnippets.Add(codeSnippet);
            StateHasChanged();
        }

        private async Task OpenComponentModal()
        {
            if (ComponentType != null)
            {
                var component = new RenderComponent<TypeBrowser>().Set(e => e.Type, ComponentType);
                var result = await modalService.ShowAsync("Component API", component, new ModalOptions { Size = ModalSize.Large });

            }
        }

        public void RemoveCodeSnippet(CodeSnippet codeSnippet)
        {
            CodeSnippets.Remove(codeSnippet);
            StateHasChanged();
        }
    }
}
