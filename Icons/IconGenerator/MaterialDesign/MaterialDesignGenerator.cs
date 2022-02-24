
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using TabBlazor;
using Tabler.Docs.Icons;

namespace IconGenerator.MaterialDesign
{
    public static class MaterialDesignGenerator
    {

        public static async Task<IEnumerable<GeneratedIcon>> GenerateIcons()
        {
            var icons = new List<GeneratedIcon>();
            var url = "https://raw.githubusercontent.com/Templarian/MaterialDesign-SVG/master/meta.json";
            var client = new HttpClient();

            var metajson = await client.GetStringAsync(url);
            var iconsMeta = JsonSerializer.Deserialize<List<MaterialDesignIcon>>(metajson);

            foreach (var iconMeta in iconsMeta)
            {
                var icon = new GeneratedIcon
                {
                    Name = iconMeta.Name,
                    Author = iconMeta.Author,
                    Tags = iconMeta.Tags
                };

                var iconUrl = $"https://unpkg.com/@mdi/svg/svg/{iconMeta.Name}.svg";
                var svgContent = await client.GetStringAsync(iconUrl);
                icon.IconType = new MDIcon(Utilities.ExtractIconElements(svgContent));
                icons.Add(icon);
                Console.WriteLine($"Icon '{icon.Name}' added");
            }

            Utilities.GenerateIconsFile("MaterialDesignIcons", icons);

            return icons;
        }


    }

    public class MaterialDesignIcon
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("codepoint")]
        public string CodePoint { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        [JsonPropertyName("aliases")]
        public List<string> Aliases { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }

}
