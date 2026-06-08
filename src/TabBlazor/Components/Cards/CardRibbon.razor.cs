
namespace TabBlazor
{
    /// <summary>Where a <see cref="CardRibbon"/> is placed on its card.</summary>
    public enum RibbonPosition
    {
        /// <summary>Top corner.</summary>
        Top,
        /// <summary>Right edge.</summary>
        Right
    }

    /// <summary>A decorative ribbon overlaid on a <c>Card</c>. Color comes from <see cref="TablerBaseComponent.BackgroundColor"/>.</summary>
    public partial class CardRibbon : TablerBaseComponent
    {
        /// <summary>The ribbon position. Defaults to <see cref="RibbonPosition.Right"/>.</summary>
        [Parameter] public RibbonPosition Position { get; set; } = RibbonPosition.Right;

        protected override string ClassNames => ClassBuilder
            .Add("ribbon")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddCompare("ribbon-top", Position, RibbonPosition.Top)
            .AddCompare("ribbon-right", Position, RibbonPosition.Right)
            .ToString();
    }
}