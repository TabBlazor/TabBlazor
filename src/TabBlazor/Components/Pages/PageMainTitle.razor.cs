using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    /// <summary>The main page heading, styled per Tabler page conventions.</summary>
    public partial class PageMainTitle : TablerBaseComponent
    {
        /// <summary>The HTML heading element to render. Defaults to <c>"h1"</c>.</summary>
        [Parameter] public string HtmlTag { get; set; } = "h1";

        protected override string ClassNames => ClassBuilder
            .Add("page-title")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}