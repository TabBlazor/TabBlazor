using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public partial class CardTitle : TablerBaseComponent
    {
        [Parameter] public string HtmlTag { get; set; } = "h1";
        protected override string ClassNames => ClassBuilder
            .Add("card-title")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}