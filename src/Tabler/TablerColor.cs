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

    public enum TablerColorType
    {
        Default,
        Outline,
        Ghost
    }

    public static class TablerColorsExtensions
    {
        public static string GetColorClass(this TablerColor colors, string type,
            TablerColorType colorType = TablerColorType.Default, string suffix = "")
        {
            var colorClass = $"{type}";
            colorClass += colorType switch
            {
                TablerColorType.Default => "",
                _ => $"-{Enum.GetName(typeof(TablerColorType), colorType)?.ToLower()}"
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