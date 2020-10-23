using Microsoft.AspNetCore.Components;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Tabler.Docs.Components.Docs
{
    public partial class DocsSnippet : ComponentBase
    {
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
               var basePath =  Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.Parent.FullName;
                const string projectName = "Tabler.Docs";
                var classPath = projectName + Class.Substring(projectName.Length).Replace(".", @"\");
                var codePath = Path.Combine(basePath, $"{classPath}.razor");

                if (File.Exists(codePath))
                {
                    Code = File.ReadAllText(codePath);
                }
               else
                {
                    Code = $"Unable to find code at {codePath}";
                }
            
            }


        }
    }
}
