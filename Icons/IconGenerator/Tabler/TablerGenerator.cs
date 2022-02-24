using IconGenerator.Converters;
using IconGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IconGenerator.Tabler
{
    public static class TablerGenerator
    {

        public static async Task<IEnumerable<GeneratedIcon>> GenerateIcons()
        {
            var icons = new List<GeneratedIcon>();
            var metaUrl = "https://raw.githubusercontent.com/tabler/tabler-icons/master/tags.json";
            var spriteUrl = "https://raw.githubusercontent.com/tabler/tabler-icons/master/tabler-sprite.svg";
            var client = new HttpClient();

            var metajson = await client.GetStringAsync(metaUrl);
            var sprite = await client.GetStringAsync(spriteUrl);

            XElement spriteSVG = XDocument.Parse(sprite).Root;
            spriteSVG.RemoveAllNamespaces();
            var iconElements = spriteSVG.Descendants().Where(e => e.Name.LocalName == "symbol");
            var options = new JsonSerializerOptions();
            options.Converters.Add(new StringDictinaryConverter<TablerIcon>());
            options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            var iconsMeta = JsonSerializer.Deserialize<Dictionary<string, TablerIcon>>(metajson);


            foreach (var iconMeta in iconsMeta)
            {
                var icon = new GeneratedIcon
                {
                    Name = iconMeta.Key,
                    Author = "Pawel",
                    Provider = IconProvider.TablerIcons,
                    Tags = iconMeta.Value.Tags
                };

                //tabler-2fa
                var elements = iconElements.FirstOrDefault(x => x.Attribute("id")?.Value == $"tabler-{icon.Name}")?.Elements();

                if (elements == null || !elements.Any())
                {
                    throw new SystemException($"Unable to find icon {icon.Name} in sprite");
                }

                //https://unpkg.com/@tabler/icons/icons/2fa.svg
                // var iconUrl = $"https://unpkg.com/@tabler/icons/icons/{iconMeta.Key}.svg";
                //var svgContent = await client.GetStringAsync(iconUrl);
                var elementsString = string.Join("", elements.Select(e => e));
                icon.Elements = Utilities.ExtractIconElements(elements);
                icons.Add(icon);
                Console.WriteLine($"Icon '{icon.Name}' added");
            }

            Utilities.GenerateIconsFile("TablerIcons.txt", icons);

            return icons;
        }


    }

    public class TablerIcon
    {

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }


        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }
    }

}

