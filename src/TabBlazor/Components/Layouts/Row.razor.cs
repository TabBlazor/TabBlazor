using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    /// <summary>A Bootstrap grid row container for <see cref="RowCol"/> columns.</summary>
    public partial class Row : TablerBaseComponent
    {
        /// <summary>When true, adds card gutters between columns. Defaults to false.</summary>
        [Parameter] public bool HasCards { get; set; }

        protected override string ClassNames => ClassBuilder
            .Add("row")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("row-cards", HasCards)
            .ToString();
    }
}