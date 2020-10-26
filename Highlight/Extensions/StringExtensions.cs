using System;

namespace Highlight.Extensions
{
    internal static class StringExtensions
    {
        public static float ToSingle(this string input, float defaultValue)
        {
            var result = default(float);
            if (Single.TryParse(input, out result)) {
                return result;
            }

            return defaultValue;
        }
    }
}