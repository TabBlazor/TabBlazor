using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    /// <summary>The title heading within a <c>Card</c> or <c>CardHeader</c>.</summary>
    public partial class CardTitle : TablerBaseComponent
    {
        /// <summary>The HTML heading element to render. Defaults to <c>"h1"</c>.</summary>
        [Parameter] public string HtmlTag { get; set; } = "h1";
        protected override string ClassNames => ClassBuilder
            .Add("card-title")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}