using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace TabBlazor
{
    /// <summary>
    /// Renders an SVG icon. Set <see cref="IconType"/> to one of the generated icon types. Size, color,
    /// stroke, rotation and animation are configurable.
    /// </summary>
    public partial class Icon : TablerBaseComponent
    {
        /// <summary>Explicit color (CSS value). When empty, uses <see cref="TablerBaseComponent.TextColor"/>.</summary>
        [Parameter] public string Color { get; set; }
        /// <summary>Icon size in pixels. Defaults to 24.</summary>
        [Parameter] public int Size { get; set; } = 24;
        /// <summary>SVG stroke width. Falls back to the icon's own width, then 2.</summary>
        [Parameter] public double? StrokeWidth { get; set; }
        /// <summary>The icon to render.</summary>
        [Parameter] public IIconType IconType { get; set; }
        /// <summary>Whether to render the filled variant, when available.</summary>
        [Parameter] public bool? Filled { get; set; }
        /// <summary>Rotation in degrees.</summary>
        [Parameter] public int Rotate { get; set; }
        /// <summary>Accessible title / tooltip text for the icon.</summary>
        [Parameter] public string Title { get; set; }
        /// <summary>Additional CSS class(es) applied to the icon.</summary>
        [Parameter] public string CssClass { get; set; }
        /// <summary>Optional animation (pulse, tada, rotate).</summary>
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
