using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public partial class Row : TablerBaseComponent
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