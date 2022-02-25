using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TabBlazor;
using TabBlazor.Services;

namespace Tabler.Docs.Components.Icons
{
    public partial class IconsList : ComponentBase
    {
        [Inject] private TablerService tabService { get; set; }
        [Inject] private ToastService toastService { get; set; }

        private List<ListIcon> icons = new List<ListIcon>();
        private List<ListIcon> filteredIcons = new List<ListIcon>();
        private List<ListIcon> selectedIcons = new List<ListIcon>();
        private int size = 24;
        private int rotate = 0;
        private string searchText;
        private string color;
        private ContentRect iconContainerRect;

        private List<IconProvider> filterProviders = new List<IconProvider>();
        private List<IconProvider> supportedProviders = new List<IconProvider>();
        protected override void OnInitialized()
        {
            LoadIcons(typeof(TablerIcons));
            LoadIcons(typeof(MDIcons));

            supportedProviders = EnumHelper.GetList<IconProvider>().Where(e => e != IconProvider.Other).ToList();
            filterProviders = supportedProviders.ToList();

            icons = icons.OrderBy(e => e.Name).ToList();
            filteredIcons = icons;

        }

        private void ToggleFilterProvider(IconProvider provider)
        {
            if (filterProviders.Contains(provider))
            {
                filterProviders.Remove(provider);
            }
            else
            {
                filterProviders.Add(provider);
            }
            SearchIcons();
        }

        private void LoadIcons(Type iconsType)
        {
            var properties = iconsType.GetProperties(BindingFlags.Public | BindingFlags.Static);

            foreach (var property in properties)
            {
                var value = property.GetValue(null);
                icons.Add(new ListIcon { Name = property.Name, IconType = (IIconType)value });
            }
        }

        private void IconContainerResized(ResizeObserverEntry resizeObserverEntry)
        {
            iconContainerRect = resizeObserverEntry.ContentRect;
        }

        private void Search(ChangeEventArgs e)
        {
            searchText = e.Value.ToString();
            SearchIcons();
        }


        private void SearchIcons()
        {

            if (!filterProviders.Any())
            {
                filteredIcons.Clear();
                return;
            }

            IEnumerable<ListIcon> query;
            query = icons;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(x => x.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase));
            }

            if (supportedProviders.Count != filterProviders.Count)
            {
                foreach (var filterProvider in filterProviders.Take(1))
                {
                    query = query.Where(e => e.IconType.Provider == filterProvider);
                }

            }


            filteredIcons = query.ToList();

        }

        private bool IsSelected(ListIcon icon) => selectedIcons.Contains(icon);

        private int GetRowCount()
        {
            var iconSize = size + 30;
            var width = iconContainerRect?.Width ?? 100;

            return (int)Math.Floor((width / iconSize));
        }

        private void SelectIcon(ListIcon icon)
        {
            if (IsSelected(icon))
            {
                selectedIcons.Remove(icon);
            }
            else
            {
                selectedIcons.Add(icon);
            }

        }

        private void ClearSelected()
        {
            selectedIcons = new List<ListIcon>();
        }

        private async Task CopyToClipboard()
        {
            var iconlist = "";
            foreach (var icon in selectedIcons)
            {
                iconlist += icon.GetStaticProperty() + Environment.NewLine;
            }

            await tabService.CopyToClipboard(iconlist);
            await toastService.AddToastAsync(new ToastModel { Title = $"{selectedIcons.Count} icons copied to clipboard", Options = new TabBlazor.ToastOptions { Delay = 2 } });
        }

    }

    public class ListIcon
    {
        public string Name { get; set; }
        public IIconType IconType { get; set; }

        public string GetStaticProperty()
        {
            return $"public static string {Name} => @\"{IconType?.Elements}\";";
        }
    }

}
