using System;

namespace Tabler
{
    public enum TablerColor
    {
        Default,
        Blue,
        Azure,
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
        Gray,
        GrayDark,
        Primary,
        Secondary,
        Success,
        Info,
        Warning,
        Danger,
        Light,
        Dark
    }

    public enum ColorType
    {
        Default,
        Outline,
        Ghost
    }

    public static class ColorsExtensions
    {
        public static string GetColorClass(this TablerColor colors, string type,
            ColorType colorType = ColorType.Default, string suffix = "")
        {
            var colorClass = $"{type}";
            colorClass += colorType switch
            {
                ColorType.Default => "",
                _ => $"-{Enum.GetName(typeof(ColorType), colorType)?.ToLower()}"
            };

            colorClass = colors switch
            {
                TablerColor.Default => "",
                TablerColor.GrayDark => $"{colorClass}-gray-dark",
                _ => $"{colorClass}-{Enum.GetName(typeof(TablerColor), colors)?.ToLower()}"
            };

            if (!string.IsNullOrWhiteSpace(suffix) && !string.IsNullOrWhiteSpace(colorClass))
                colorClass += $"-{suffix}";

            return colorClass;
        }
    }
}