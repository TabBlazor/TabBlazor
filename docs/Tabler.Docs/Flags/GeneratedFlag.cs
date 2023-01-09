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
        public string DotNetProperty => $"public static IFlagType {GetSafeName()} => new {FlagType.ClassName}(@\"{FlagType?.Elements}\");";

        public string GetSafeName()
        {
            var safeName = Name;
            safeName = safeName.Replace("'", "");
            safeName = safeName.Replace("(", "");
            safeName = safeName.Replace(")", "");
            safeName = safeName.Replace(",", "");
            safeName = safeName.Replace(".", "");
            safeName = safeName.Replace(" ", "_");
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

  

}
