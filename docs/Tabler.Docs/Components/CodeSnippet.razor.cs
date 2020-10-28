using ColorCode;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using Tabler.Docs.Services;

namespace Tabler.Docs.Components
{
    public partial class CodeSnippet : ComponentBase
    {
        [Inject] ICodeSnippetService CodeSnippetService { get; set; }
        [CascadingParameter] DocsExample DocsExample { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public bool SetBackground { get; set; }
        [Parameter] public string ClassName { get; set; }
        [Parameter] public RenderFragment Description { get; set; }
        [Parameter] public RenderFragment Example { get; set; }

        protected string Code;

        public Guid Id { get; internal set; } = Guid.NewGuid();

        protected override async Task OnInitializedAsync()
        {
            DocsExample.AddCodeSnippet(this);

            if (!string.IsNullOrWhiteSpace(ClassName) && string.IsNullOrEmpty(Code))
            {
                var formatter = new HtmlClassFormatter();
            
                var html = await CodeSnippetService.GetCodeSnippet(ClassName);
                var cSharp = "";

                var index = html.IndexOf("@code {");
                if (index > 0)
                {
                    cSharp = html.Substring(index);
                    html = html.Substring(0, index);
                }

                var code = formatter.GetHtmlString(html, Languages.Html);

                if (!string.IsNullOrWhiteSpace(cSharp))
                {
                    code = code + formatter.GetHtmlString(cSharp, Languages.CSharp);
                }

                Code = code;
                
            }
        }

        private string ExampleBackground()
        {
            return SetBackground ? "example-bg" : "";
        }

        public void Dispose()
        {
            DocsExample.RemoveCodeSnippet(this);
        }

    }
}
