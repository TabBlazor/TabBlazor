
namespace TabBlazor
{
    public class GeneratedFlag
    {
        public string Name { get; set; }

        public IFlagType FlagType { get; set; }
        public string DotNetProperty { 
        get
            {
                if (FlagType.Country == null)
                {
                    return $"public static IFlagType {GetSafeName()} => new {FlagType.ClassName}(@\"{FlagType.Elements}\");";
                }
                return $"public static IFlagType {GetSafeName()} => new {FlagType.ClassName}(@\"{FlagType.Elements}\", new TabBlazor.Country(\"{FlagType.Country.Name}\", \"{FlagType.Country.Alpha2}\", \"{FlagType.Country.Alpha3}\", {FlagType.Country.Numeric}));";
            }

        } 
      
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
