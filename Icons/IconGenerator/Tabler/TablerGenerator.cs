using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nager.Country;
using TabBlazor;
using Tabler.Docs.Icons;

namespace IconGenerator.Tabler;

public static class TablerGenerator
{
    public static async Task<List<GeneratedFlag>> GenerateFlags()
    {
        var generatedFlags = new List<GeneratedFlag>();
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.Add(new("TabBlazor", "1"));

        var countryProvider = new CountryProvider();
        var countries = countryProvider.GetCountries();

        var repo = "tabler/tabler";
        var contentsUrl = $"https://api.github.com/repos/{repo}/contents/src/img/flags";
        var contentsJson = await httpClient.GetStringAsync(contentsUrl);

        var entries = JsonSerializer.Deserialize<List<GitHubEntry>>(contentsJson);

        foreach (var file in entries.Where(e => e.Name.EndsWith(".svg")))
        {
            Console.WriteLine(file.Name);


            var countryAbbrevation = file.Name.Substring(0, file.Name.Length - 4);
            var generatedFlag = new GeneratedFlag();

            var country =
                countries.FirstOrDefault(e => e.Alpha2Code.ToString().ToLower() == countryAbbrevation.ToLower());

            if (country != null)
            {
                generatedFlag.Name = country.CommonName;
            }
            else
            {
                generatedFlag.Name = countryAbbrevation;
            }

            var content = await httpClient.GetStringAsync(file.DownloadUrl);
            var flagSvg = XDocument.Parse(content).Root;

            int width;
            int height;

            var viewBox = flagSvg.Attribute("viewBox")?.Value;

            if (viewBox != null)
            {
                var arr = viewBox.Split(' ');
                width = int.Parse(arr[2]);
                height = int.Parse(arr[3]);
            }
            else
            {
                width = int.Parse(flagSvg.Attribute("width").Value);
                height = int.Parse(flagSvg.Attribute("height").Value);
            }

            flagSvg.RemoveAllNamespaces();

            generatedFlag.FlagType = new TablerFlag(Utilities.ExtractFlagElements(flagSvg), width, height);

            if (country != null)
            {
                generatedFlag.FlagType.Country = new(country.CommonName, country.Alpha2Code.ToString(),
                    country.Alpha3Code.ToString(), country.NumericCode);
            }

            generatedFlags.Add(generatedFlag);
        }

        Utilities.GeneratFlagsFile("TablerFlags", generatedFlags);

        return generatedFlags;
    }

    public static Task<List<GeneratedIcon>> GenerateIcons()
    {
        var icons = new List<GeneratedIcon>();
        var spriteLocal =
            File.ReadAllText(@"C:\Code\Github\TabBlazor\Icons\IconGenerator\Tabler\sprites\dist\tabler-sprite.svg");

        var spriteSVG = XDocument.Parse(spriteLocal).Root;
        spriteSVG.RemoveAllNamespaces();
        var iconElements = spriteSVG.Descendants().Where(e => e.Name.LocalName == "symbol").ToList();

        foreach (var elements in iconElements)
        {
            var icon = new GeneratedIcon
            {
                Name = elements.Attribute("id").Value.Substring(7), Author = "Paweł Kuna", Tags = []
            };

            var isFilled = icon.Name.StartsWith("filled", StringComparison.InvariantCultureIgnoreCase);

            icon.IconType = new TabBlazor.TablerIcon(Utilities.ExtractIconElements(elements.Elements()), isFilled);
            icons.Add(icon);
            Console.WriteLine($"Icon '{icon.Name}' added");
        }

        Utilities.GenerateIconsFile("TablerIcons", icons);

        return Task.FromResult(icons);
    }
}

public class TablerIcon
{
    [JsonPropertyName("tags")] public List<string> Tags { get; set; }

    [JsonPropertyName("version")] public string Version { get; set; }

    [JsonPropertyName("category")] public string Category { get; set; }
}