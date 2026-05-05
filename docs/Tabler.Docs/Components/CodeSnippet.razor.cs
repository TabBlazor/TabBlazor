using ColorCode;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
        public string ClassName { get; set; }
        [Parameter] public Type Component { get; set; }
        [Parameter] public RenderFragment Description { get; set; }

        protected string Code;

        public Guid Id { get; internal set; } = Guid.NewGuid();

        protected override async Task OnInitializedAsync()
        {
            ClassName ??= Component.ToString();
            
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

                html = html.TrimEnd();

                var razorMap = new Dictionary<string, string>();
                var n = 0;
                var preserved = Regex.Replace(html, @"@[A-Za-z_][A-Za-z0-9_]*", m =>
                {
                    var key = $"__RZ{n++}__";
                    razorMap[key] = m.Value;
                    return key;
                });

                var code = formatter.GetHtmlString(preserved, Languages.Html);

                foreach (var kvp in razorMap)
                {
                    code = code.Replace(kvp.Key, $"<span class=\"razor\">{kvp.Value}</span>");
                }

                if (!string.IsNullOrWhiteSpace(cSharp))
                {
                    code += "<div class='mt-1'>" + formatter.GetHtmlString(cSharp.TrimEnd(), Languages.CSharp) + "</div>";
                }

                code = HighlightRazor(code);

                Code = code;
                
            }
        }

        private string HighlightRazor(string code)
        {
            var keywords = new List<string> { "@code", "@inject" };
           
            var result = code;
            foreach (var keyword in keywords)
            {
                // var rx = new Regex($@"^{keyword}\s");
                // result = rx.Replace(result, @"<span class=""razor"">{keyword}</span>");
                result = result.Replace(keyword, $@"<span class=""razor"">{keyword}</span>");
            }

         
            return result;

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
