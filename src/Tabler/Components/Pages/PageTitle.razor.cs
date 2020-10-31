using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public partial class PageTitle : TablerBaseComponent
    {
        [Parameter] public string HtmlTag { get; set; } = "h1";

        protected override string ClassNames => ClassBuilder
            .Add("page-title")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}