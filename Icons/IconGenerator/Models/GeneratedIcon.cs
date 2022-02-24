using System;
using System.Collections.Generic;
using System.Linq;

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
        public string DotNetProperty => $"public static IIcon {GetSafeName()} => new {GetClassName()}(@\"{Elements}\");";

        private string GetClassName() {

            return Provider switch
            {
                IconProvider.TablerIcons => "TablerIcon",
                IconProvider.MaterialDesignIcons => "MDIcon",
                _ => throw new SystemException($"Provider {Provider} is unknown"),
            };
        }
            

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
