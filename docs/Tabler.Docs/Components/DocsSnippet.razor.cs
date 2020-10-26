using Highlight;
using Highlight.Engines;
using Microsoft.AspNetCore.Components;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Tabler.Docs.Highlight;
using Tabler.Docs.Services;

namespace Tabler.Docs.Components
{
    public partial class DocsSnippet : ComponentBase
    {
        [Inject] ICodeSnippetService CodeSnippetService { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public RenderFragment Description { get; set; }

        [Parameter]  public RenderFragment Example { get; set; }

        [Parameter] public int Columns { get; set; } = 0;

        [Parameter] public bool Centered { get; set; }

        [Parameter] public string Class { get; set; }

        protected string Code;
        protected override async Task OnInitializedAsync()
        {

            if (!string.IsNullOrWhiteSpace(Class) && string.IsNullOrEmpty(Code))
            {
                // var sourceColorer = new SourceColorer { AddStyleDefinition = false };
                //sourceColorer.Keywords.Add("<");
                //sourceColorer.Keywords.Add(">");

                var highlight = new Highlighter(new HtmlEngine { UseCss = true });
               

                Code = highlight.Highlight("HTML", await CodeSnippetService.GetCodeSnippet(Class));

                //var test = highlight.Highlight("HTML",Code);
                var kalle = "dd";
              


            }


        }
    }
}
