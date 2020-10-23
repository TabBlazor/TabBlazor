using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public partial class TablerRow : TablerBaseComponent
    {
        [Parameter] public bool HasCards { get; set; }

        protected override string ClassNames => ClassBuilder
            .Add("row")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("row-cards", HasCards)
            .ToString();
    }
}