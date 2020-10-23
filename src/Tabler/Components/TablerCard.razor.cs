using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public enum TablerCardSize
    {
        Default,
        Small,
        Medium,
        Large
    }

    public enum TablerCardStatusPosition
    {
        Left,
        Top,
        Bottom
    }
    public partial class TablerCard : TablerBaseComponent
    {
        [Parameter] public TablerCardSize Size { get; set; } = TablerCardSize.Default;
        [Parameter] public bool IsStacker { get; set; }
        [Parameter] public TablerColor StatusTop { get; set; } = TablerColor.Default;
        [Parameter] public TablerColor StatusLeft { get; set; } = TablerColor.Default;
        [Parameter] public TablerColor StatusBottom { get; set; } = TablerColor.Default;
        [Parameter] public string LinkTo { get; set; }

        protected string HtmlTag => string.IsNullOrWhiteSpace(LinkTo)
            ? "div"
            : "a";

        protected string Href => !string.IsNullOrWhiteSpace(LinkTo)
            ? LinkTo
            : null;

        protected override string ClassNames => ClassBuilder
            .Add("card")
            .AddIf("card-stacked", IsStacker)
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddCompare("card-sm", Size, TablerCardSize.Small)
            .AddCompare("card-md", Size, TablerCardSize.Medium)
            .AddCompare("card-lg", Size, TablerCardSize.Large)
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