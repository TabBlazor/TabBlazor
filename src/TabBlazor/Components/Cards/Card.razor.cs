using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    /// <summary>
    /// Overall sizing of a <see cref="Card"/>, controlling its padding scale.
    /// </summary>
    public enum CardSize
    {
        /// <summary>Standard card padding.</summary>
        Default,
        /// <summary>Compact card with reduced padding.</summary>
        Small,
        /// <summary>Medium card padding.</summary>
        Medium,
        /// <summary>Large card with increased padding.</summary>
        Large
    }

    /// <summary>
    /// Edge along which a card's colored status indicator is rendered.
    /// </summary>
    public enum CardStatusPosition
    {
        /// <summary>Status bar along the left edge.</summary>
        Left,
        /// <summary>Status bar along the top edge.</summary>
        Top,
        /// <summary>Status bar along the bottom edge.</summary>
        Bottom
    }

    /// <summary>
    /// Tabler card container that groups related content. Compose with
    /// <see cref="CardHeader"/>, <see cref="CardBody"/>, <see cref="CardFooter"/> and related child components.
    /// </summary>
    public partial class Card : TablerBaseComponent
    {
        /// <summary>Padding scale of the card. Defaults to <see cref="CardSize.Default"/>.</summary>
        [Parameter] public CardSize Size { get; set; } = CardSize.Default;

        /// <summary>When <c>true</c>, renders the card with a stacked (layered) visual effect. Defaults to <c>false</c>.</summary>
        [Parameter] public bool Stacked { get; set; }

        /// <summary>Color of a status bar drawn along the top edge. Defaults to <see cref="TablerColor.Default"/>, which hides the bar.</summary>
        [Parameter] public TablerColor StatusTop { get; set; } = TablerColor.Default;

        /// <summary>Color of a status bar drawn along the start (left) edge. Defaults to <see cref="TablerColor.Default"/>, which hides the bar.</summary>
        [Parameter] public TablerColor StatusStart { get; set; } = TablerColor.Default;

        /// <summary>When set, renders the card as an anchor linking to this URL instead of a plain <c>div</c>.</summary>
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