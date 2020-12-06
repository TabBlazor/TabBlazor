using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace TabBlazor
{
    public partial class Icon : TablerBaseComponent
    {
        [Parameter] public string Color { get; set; }
        [Parameter] public int Size { get; set; } = 24;
        [Parameter] public double StrokeWidth { get; set; } = 2;
        [Parameter] public string Elements { get; set; }
        [Parameter] public bool Filled { get; set; }

        protected override string ClassNames => ClassBuilder
             .AddIf($"{TextColor.GetColorClass("text")}", string.IsNullOrWhiteSpace(Color))
            .AddIf("icon-filled", Filled)
            .ToString();
    }
}
