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

        private List<FlagMember> flagMembers = new();

        public List<FlagMember> AllFlagMembers => flagMembers;
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
                        flagMembers.Add(new FlagMember { Name = prop.Name, FlagType = (IFlagType)prop.GetValue(null) });
                    }
                }
            }

            countryFlagTypes = flagMembers.Select(e => e.FlagType).Where(f => f.Country != null).ToList();
        }
    }

    public class FlagMember
    {
        public string Name { get; set; }
        public IFlagType FlagType { get; set; }

    }

}
