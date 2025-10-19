﻿using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace TabBlazor
{
    public partial class Icon : TablerBaseComponent
    {
        [Parameter] public string Color { get; set; }
        [Parameter] public int Size { get; set; } = 24;
        [Parameter] public double? StrokeWidth { get; set; }
        [Parameter] public IIconType IconType { get; set; }
        [Parameter] public bool? Filled { get; set; }
        [Parameter] public int Rotate { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public string CssClass { get; set; }
        [Parameter] public IconAnimation Animation { get; set; }


        //private bool filled => Filled ?? IconType?.Filled ?? false;
        private double strokeWidth => StrokeWidth ?? IconType?.StrokeWidth ?? 2;
        private string elements => IconType?.Elements;

        //private string FilledString => filled ? "currentColor" : "none";

        protected override string ClassNames => ClassBuilder
            .AddIf($"{TextColor.GetColorClass("text")}", string.IsNullOrWhiteSpace(Color))
            .AddIf("cursor-pointer", OnClick.HasDelegate)
            .AddIf(CssClass, !string.IsNullOrWhiteSpace(CssClass))
            .Add(GetAnimationClass())
            .ToString();


        private string GetAnimationClass()
        {
            return Animation switch
            {
                IconAnimation.Pulse => "icon-pulse",
                IconAnimation.Tada => "icon-tada",
                IconAnimation.Rotate => "icon-rotate",
                _ => "",
            };
        }
    }
}
