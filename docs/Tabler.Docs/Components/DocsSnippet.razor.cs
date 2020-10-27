using ColorCode;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Tabler.Docs.Services;

namespace Tabler.Docs.Components
{
    public partial class DocsSnippet : ComponentBase
    {
        [Inject] ICodeSnippetService CodeSnippetService { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public RenderFragment Description { get; set; }

        [Parameter] public RenderFragment Example { get; set; }

        [Parameter] public int Columns { get; set; } = 0;

        [Parameter] public bool Centered { get; set; }

        [Parameter] public string Class { get; set; }

        protected string Code;
        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(Class) && string.IsNullOrEmpty(Code))
            {
                var formatter = new HtmlClassFormatter();
                Code = formatter.GetHtmlString(await CodeSnippetService.GetCodeSnippet(Class), Languages.Html);
            }
        }
    }
}
