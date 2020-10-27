using ColorCode;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Tabler.Docs.Services;

namespace Tabler.Docs.Components
{
    public partial class CodeSnippet : ComponentBase
    {
        [Inject] ICodeSnippetService CodeSnippetService { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public string ClassName { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }

        protected string Code;
        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(ClassName) && string.IsNullOrEmpty(Code))
            {
                var formatter = new HtmlClassFormatter();
                Code = formatter.GetHtmlString(await CodeSnippetService.GetCodeSnippet(ClassName), Languages.Html);
            }
        }
    }
}
