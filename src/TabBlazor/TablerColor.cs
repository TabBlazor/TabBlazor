using System;

namespace TabBlazor
{
    /// <summary>
    /// The Tabler color palette used throughout the library for backgrounds, text, borders and component
    /// accents. Combine with <see cref="ColorsExtensions.GetColorClass"/> to produce CSS classes.
    /// </summary>
    public enum TablerColor
    {
        /// <summary>No explicit color; inherits the component's default styling.</summary>
        Default,
        Blue,
        //BlueLight, 
        Azure,
        //AzureLight,
        Indigo,
        Purple,
        Pink,
        Red,
        Orange,
        Yellow,
        Lime,
        Green,
        Teal,
        Cyan,
        White,
        //Gray,
        //GrayDark,
        Primary,
        Secondary,
        Success,
        Info,
        Warning,
        Danger,
        Light,
        Dark
    }

    /// <summary>
    /// The visual style variant applied to a <see cref="TablerColor"/>.
    /// </summary>
    public enum ColorType
    {
        /// <summary>Solid filled color.</summary>
        Default,
        /// <summary>Outlined: colored border and text, transparent background.</summary>
        Outline,
        /// <summary>Ghost: colored text only, no border or background until hovered.</summary>
        Ghost
    }

    /// <summary>
    /// Extension methods for turning a <see cref="TablerColor"/> into Tabler/Bootstrap CSS class names.
    /// </summary>
    public static class ColorsExtensions
    {
        /// <summary>
        /// Builds a CSS class for the given color, e.g. <c>btn-primary</c> or <c>bg-blue-lt</c>.
        /// </summary>
        /// <param name="color">The color to render. <see cref="TablerColor.Default"/> yields no color suffix.</param>
        /// <param name="type">The class prefix, e.g. <c>btn</c>, <c>bg</c>, <c>text</c>.</param>
        /// <param name="colorType">The variant (solid, outline, ghost).</param>
        /// <param name="suffix">Optional trailing modifier appended to the class.</param>
        /// <returns>The composed CSS class, or an empty string when <paramref name="color"/> is <see cref="TablerColor.Default"/>.</returns>
        public static string GetColorClass(this TablerColor color, string type,
            ColorType colorType = ColorType.Default, string suffix = "")
        {
            var colorClass = $"{type}";

            colorClass += colorType switch
            {
                ColorType.Default => "",
                _ => $"-{Enum.GetName(typeof(ColorType), colorType)?.ToLower()}"
            };

            colorClass = color switch
            {
                TablerColor.Default => "",
                //TablerColor.GrayDark => $"{colorClass}-gray-dark",
                _ => $"{colorClass}-{Enum.GetName(typeof(TablerColor), color)?.ToLower()}"
            };

            if (color != TablerColor.Light && colorClass.ToLower().EndsWith("light"))
            {
                colorClass = colorClass.Replace("light", "-lt", StringComparison.InvariantCultureIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(suffix) && !string.IsNullOrWhiteSpace(colorClass))
                colorClass += $"-{suffix}";

            return colorClass;
        }
    }
}