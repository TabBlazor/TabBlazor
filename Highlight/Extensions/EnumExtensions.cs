using System;

namespace Highlight.Extensions
{
    internal static class Enum<T> where T : struct
    {
        public static T Parse(string value, T defaultValue)
        {
            return Parse(value, defaultValue, false);
        }

        public static T Parse(string value, T defaultValue, bool ignoreCase)
        {
            var result = default(T);
            if (Enum.TryParse(value, ignoreCase, out result)) {
                return result;
            }

            return defaultValue;
        }
    }
}