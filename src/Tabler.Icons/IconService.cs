using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Tabler.Icons
{
    public class IconService
    {
        private readonly HttpClient httpClient = new HttpClient();
        private Dictionary<string, string> iconsCache;
        private bool isClientSide; 

        private bool isLoading = false;
        private XElement iconSprite = null;


        public IconService()
        {
        //    this.httpClient = httpClient;
            isClientSide = Type.GetType("Mono.Runtime") != null;

        }

        public async Task<string> GetIcon(string iconName)
        {

           while (isLoading)
            {
                await Task.Delay(25);
            }

           if (iconsCache == null)
            {
                await LoadIcons();
            }

            var tablerName = "tabler-" + iconName;
            if (iconsCache.ContainsKey(tablerName))
            {
                return iconsCache[tablerName];
            }

            return string.Empty;


        }

        private async Task LoadIcons()
        {
             isLoading = true;
            
            if (iconsCache == null) { iconsCache = new Dictionary<string, string>(); }
            
            var url = $"/_content/Tabler.Icons/icons/blazor-tabler-sprite.svg";
            url = "https://localhost:44362/_content/Tabler.Icons/icons/blazor-tabler-sprite.svg";

            var stream = await httpClient.GetStreamAsync(url);

            var token = new CancellationToken();
            iconSprite = (await XDocument.LoadAsync(stream, LoadOptions.None, token)).Root;

            var iconElements = iconSprite.Descendants().Where(e => e.Name.LocalName == "symbol");

            foreach (var iconElement in iconElements)
            {
                var iconName = iconElement.Attributes().First(e => e.Name == "id").Value;
                var data = string.Concat(iconElement.Nodes().Select(x => x.ToString()).ToArray());
                if (!iconsCache.ContainsKey(iconName))
                {
                    iconsCache.Add(iconName, data);
                }
                
            }

            isLoading = false;
        }

        public async Task DownloadAllIcons()
        {
            //var icons = Enum.GetValues(typeof(TablerIconType)).Cast<TablerIconType>().ToList();
            //var tasks = new List<Task>();
            //foreach (var icon in icons)
            //{
            //    tasks.Add(GetIconElementsAsync(icon.GetIconName()));
            //}
            //await Task.WhenAll(tasks);
        }

        public async Task<string> GetIconElementsAsync(string iconName)
        {
            if (!iconsCache.ContainsKey(iconName)) {
                var url = $"_content/tabler-icons-blazor/icons/tabler/{iconName}.svg";

                var stream = await httpClient.GetStreamAsync(url);
                var svg = XDocument.Load(stream).Root;
                var data = string.Concat(svg.Nodes().Select(x => x.ToString()).ToArray());

                if (!iconsCache.ContainsKey(iconName))
                {
                    iconsCache.Add(iconName, data);
                }
                   
            }

            return iconsCache[iconName];
        }

    }
}
