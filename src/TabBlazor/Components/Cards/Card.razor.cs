using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public enum CardSize
    {
        Default,
        Small,
        Medium,
        Large
    }

    public enum CardStatusPosition
    {
        Left,
        Top,
        Bottom
    }
    public partial class Card : TablerBaseComponent
    {
        [Parameter] public CardSize Size { get; set; } = CardSize.Default;
        [Parameter] public bool Stacked { get; set; }
        [Parameter] public TablerColor StatusTop { get; set; } = TablerColor.Default;
        [Parameter] public TablerColor StatusStart { get; set; } = TablerColor.Default;
      
        [Parameter] public string LinkTo { get; set; }

        protected string HtmlTag => string.IsNullOrWhiteSpace(LinkTo)
            ? "div"
            : "a";

        protected string Href => !string.IsNullOrWhiteSpace(LinkTo)
            ? LinkTo
            : null;

        protected override string ClassNames => ClassBuilder
            .Add("card")
            .AddIf("card-stacked", Stacked)
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddCompare("card-sm", Size, CardSize.Small)
            .AddCompare("card-md", Size, CardSize.Medium)
            .AddCompare("card-lg", Size, CardSize.Large)
            .ToString();

        protected string StatusClassNames(string position, TablerColor color)
        {
            return ClassBuilder
                .Add($"card-status-{position}")
                .Add(color.GetColorClass("bg"))
                .ToString();
        }
    }
}