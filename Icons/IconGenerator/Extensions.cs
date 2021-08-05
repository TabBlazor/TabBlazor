using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace IconGenerator
{
    public static class Extensions
    {
        public static string FirstCharacterToUpperCase(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) { return text; }

            if (text.Length == 1)
            {
                return char.ToUpper(text[0]).ToString();
            }
                    
               return char.ToUpper(text[0]) + text.Substring(1);
        }

    }
}
