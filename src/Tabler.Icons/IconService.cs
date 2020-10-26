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

    public interface IIconService
    {
        Task<string> GetIcon(string iconName);
    }

    public class FakeIconService : IIconService
    {
        public async Task<string> GetIcon(string iconName)
        {
            var sampleIcon = @"<path stroke='none' d='M0 0h24v24H0z' fill='none'/>
                              <path d='M8 8a3.5 3 0 0 1 3.5 -3h1a3.5 3 0 0 1 3.5 3a3 3 0 0 1 -2 3a3 4 0 0 0 -2 4' />
                              <line x1='12' y1='19' x2='12' y2='19.01' />";
            return sampleIcon;
        }
    }

    public class IconService : IIconService
    {


        //private readonly HttpClient httpClient = new HttpClient();
        private Dictionary<string, string> iconsCache;
        private bool isClientSide;

        private bool isLoading = false;
        private XElement iconSprite = null;
        private readonly IHttpClientFactory httpClientFactory;

        public IconService(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
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

            var httpClient = httpClientFactory.CreateClient("Local");
            try
            {
                if (iconsCache == null) { iconsCache = new Dictionary<string, string>(); }
                var url = $"_content/Tabler.Icons/icons/blazor-tabler-sprite.svg";
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
            catch (Exception)
            {


            }
        }

    
    

    }
}
