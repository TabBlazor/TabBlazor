using System.Reflection;
using Microsoft.Extensions.Options;

namespace TabBlazor;

public class FlagService
{
    public FlagService(IOptions<TablerOptions> options)
    {
        foreach (var a in options.Value.AssemblyScanFilter())
        {
            foreach (var t in a.GetTypes())
            {
                foreach (var prop in t.GetProperties(BindingFlags.Static | BindingFlags.Public)
                             .Where(e => e.PropertyType == typeof(IFlagType)))
                {
                    GeneratedFlags.Add(
                        new() { Name = prop.Name, FlagType = (IFlagType)prop.GetValue(null) });
                }
            }
        }

        CountryFlagTypes = GeneratedFlags.Select(e => e.FlagType).Where(f => f.Country != null).ToList();
    }

    private List<GeneratedFlag> GeneratedFlags { get; set; } = [];
    private List<IFlagType> CountryFlagTypes { get; set; }
    public List<GeneratedFlag> AllFlags => GeneratedFlags;
    

    public IFlagType GetFlagType(int numericCode)
    {
        return CountryFlagTypes.FirstOrDefault(e => e.Country.Numeric == numericCode);
    }

    public IFlagType GetFlagType(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return null;
        }

        if (countryCode.Length == 2)
        {
            return CountryFlagTypes.FirstOrDefault(e => e.Country.Alpha2.ToLower() == countryCode.ToLower());
        }

        if (int.TryParse(countryCode, out var numeric))
        {
            return GetFlagType(numeric);
        }

        return CountryFlagTypes.FirstOrDefault(e => e.Country?.Alpha3.ToLower() == countryCode.ToLower());
    }
}