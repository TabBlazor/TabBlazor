using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace TabBlazor
{
    public partial class Icon : TablerBaseComponent
    {
        [Parameter] public string Color { get; set; }
        [Parameter] public int Size { get; set; } = 24;
        [Parameter] public double? StrokeWidth { get; set; }
        [Parameter] public string Elements { get; set; }
        [Parameter] public bool Filled { get; set; } //Should this be null? breaking change
        [Parameter] public int Rotate { get; set; }

        [Parameter] public IIconType IconType { get; set; }


        private bool filled =>  IconType?.Filled == true ? true : Filled;
        private double strokeWidth => StrokeWidth ?? IconType?.StrokeWidth ?? 2;
        private string elements => IconType?.Elements ?? Elements;

        private string FilledString => filled ? "currentColor" : "none";

        protected override string ClassNames => ClassBuilder
            .AddIf($"{TextColor.GetColorClass("text")}", string.IsNullOrWhiteSpace(Color))
            //.AddIf("icon-filled", Filled)
            .AddIf("cursor-pointer", OnClick.HasDelegate)
            .ToString();
    }
}
