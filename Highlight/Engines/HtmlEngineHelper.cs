using System;
using System.Drawing;
using System.Text;
using Highlight.Patterns;

namespace Highlight.Engines
{
    internal static class HtmlEngineHelper
    {
        public static string CreateCssClassName(string definition, string pattern)
        {
            var cssClassName = definition
                .Replace("#", "sharp")
                .Replace("+", "plus")
                .Replace(".", "dot")
                .Replace("-", "");

            return String.Concat(cssClassName, pattern);
        }

        public static string CreatePatternStyle(Style style)
        {
            return CreatePatternStyle(style.Colors, style.Font);
        }

        public static string CreatePatternStyle(ColorPair colors, Font font)
        {
            var patternStyle = new StringBuilder();
            if (colors != null) {
                if (colors.ForeColor != Color.Empty) {
                    patternStyle.Append("color: " + colors.ForeColor.Name + ";");
                }
                if (colors.BackColor != Color.Empty) {
                    patternStyle.Append("background-color: " + colors.BackColor.Name + ";");
                }
            }

            if (font != null) {
                if (font.Name != null) {
                    patternStyle.Append("font-family: " + font.Name + ";");
                }
                if (font.Size > 0f) {
                    patternStyle.Append("font-size: " + font.Size + "px;");
                }
                if (font.Style == FontStyle.Regular) {
                    patternStyle.Append("font-weight: normal;");
                }
                if (font.Style == FontStyle.Bold) {
                    patternStyle.Append("font-weight: bold;");
                }
            }

            return patternStyle.ToString();
        }
    }
}