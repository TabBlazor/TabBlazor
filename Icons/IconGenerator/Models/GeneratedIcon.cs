using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IconGenerator.Models
{
    public class GeneratedIcon
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public List<string> Tags { get; set; }
        public string Category { get; set; }
        public string Elements { get; set; }
        public IconProvider Provider { get; set; }
        public string DotNetProperty => $"public static IIconType {GetSafeName()} => new MDIcon(@\"{Elements}\");";

        //  public static IIconType Ab_test => new MDIcon(@"<path d='M4 2A2 2 0 0 0 2 4V12H4V8H6V12H8V4A2 2 0 0 0 6 2H4M4 4H6V6H4M22 15.5V14A2 2 0 0 0 20 12H16V22H20A2 2 0 0 0 22 20V18.5A1.54 1.54 0 0 0 20.5 17A1.54 1.54 0 0 0 22 15.5M20 20H18V18H20V20M20 16H18V14H20M5.79 21.61L4.21 20.39L18.21 2.39L19.79 3.61Z' />");


        public string GetSafeName()
        {
            var safeName = Name;
            safeName = safeName.Replace("-", "_");
            if (char.IsDigit(safeName.ToCharArray().First()))
            {
                safeName = "_" + safeName;
            }
            else
            {
                safeName = FirstCharacterToUpperCase(safeName);
            }

            return safeName;
        }

   
        private static string FirstCharacterToUpperCase(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) { return text; }

            if (text.Length == 1)
            {
                return char.ToUpper(text[0]).ToString();
            }

            return char.ToUpper(text[0]) + text.Substring(1);
        }

    }

    public enum IconProvider
    {
        TablerIcons,
        MaterialDesignIcons
    }

}
