using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace IconGenerator
{
    public static class Extensions
    {
       
        public static void RemoveAllNamespaces(this XElement element)
        {
            element.Name = element.Name.LocalName;

            foreach (var node in element.DescendantNodes())
            {
                var xElement = node as XElement;
                if (xElement != null)
                {
                    RemoveAllNamespaces(xElement);
                }
            }
        }

        public static string GetIconName(this string iconName)
        {
            iconName = iconName.Replace("-", "_");
            if (char.IsDigit(iconName.ToCharArray().First()))
            {
                iconName = "_" + iconName;
            }
            else
            {
                iconName = iconName.FirstCharacterToUpperCase();
            }

            return iconName;
        }


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
