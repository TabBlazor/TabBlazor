using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace TabBlazor
{
    public class FlagService
    {

        private List<GeneratedFlag> generatedFlags = new();

        public List<GeneratedFlag> AllFlags => generatedFlags;
        public List<IFlagType> countryFlagTypes { get; set; }


        public IFlagType GetFlagType(int numericCode)
        {
            return countryFlagTypes.FirstOrDefault(e => e.Country.Numeric == numericCode);
        }

        public IFlagType GetFlagType(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode)) { return null; }

            if (countryCode.Length == 2)
            {
                return countryFlagTypes.FirstOrDefault(e => e.Country.Alpha2.ToLower() == countryCode.ToLower());
            }

            if (int.TryParse(countryCode, out var numeric))
            {
                return GetFlagType(numeric);
            }

            return countryFlagTypes.FirstOrDefault(e => e.Country?.Alpha3.ToLower() == countryCode.ToLower());
        }

        public FlagService()
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in a.GetTypes())
                {
                    foreach (var prop in t.GetProperties(BindingFlags.Static | BindingFlags.Public).Where(e => e.PropertyType == typeof(IFlagType)))
                    {
                        generatedFlags.Add(new GeneratedFlag { Name = prop.Name, FlagType = (IFlagType)prop.GetValue(null) });
                    }
                }
            }

            countryFlagTypes = generatedFlags.Select(e => e.FlagType).Where(f => f.Country != null).ToList();
        }
    }

  

}
