using System;
using System.Collections.Generic;
using System.Linq;
using TabBlazor;

namespace Tabler.Docs.Icons
{
    public class GeneratedFlag
    {
        public string Name { get; set; }

        public IFlagType FlagType { get; set; }
        public string DotNetProperty { 
        get
            {
                if (Country == null)
                {
                    return $"public static IFlagType {GetSafeName()} => new {FlagType.ClassName}(@\"{FlagType?.Elements}\");";
                }

                return $"public static IFlagType {GetSafeName()} => new {FlagType.ClassName}(@\"{FlagType?.Elements}\", new TabBlazor.Country(\"{Country.Name}\", \"{Country.Alpha2}\", \"{Country.Alpha3}\", {Country.Numeric}));";
            }

        } 
        
        public TabBlazor.Country Country { get; set; }

        public string GetSafeName()
        {
            var safeName = Name;
            safeName = safeName.Replace(" or ", " Or ");
            safeName = safeName.Replace(" and ", " And ");
            safeName = safeName.Replace("'", "");
            safeName = safeName.Replace("(", "");
            safeName = safeName.Replace(")", "");
            safeName = safeName.Replace(",", "");
            safeName = safeName.Replace(".", "");
            safeName = safeName.Replace(" ", "");
            safeName = safeName.Replace("-", "");

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



}
