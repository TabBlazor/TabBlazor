using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TabBlazor.Services;

namespace Tabler.Docs.Components.Icons
{
    public partial class IconsList : ComponentBase
    {
        [Inject] private TablerService tabService { get; set; }
        [Inject] private ToastService toastService { get; set; }

        private List<Icon> icons = new List<Icon>();
        private List<Icon> filteredIcons = new List<Icon>();
        private List<Icon> selectedIcons = new List<Icon>();
        private int size = 24;
        private double strokeWidth = 2;
        private string searchText;
        private string color;
        protected override void OnInitialized()
        {
            LoadIcons();
            filteredIcons = icons;
        }

        private void LoadIcons()
        {
            var properties = typeof(DemoIcons).GetProperties(BindingFlags.Public | BindingFlags.Static);

            foreach (var property in properties)
            {
                var value = property.GetValue(null).ToString();
                icons.Add(new Icon { Name = property.Name, Elements = value });
            }
        }

        private void SearchIcons(ChangeEventArgs e)
        {
            searchText = e.Value.ToString();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredIcons = icons;
            }
            else
            {
                filteredIcons = icons.Where(x => x.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }
        }

        private bool IsSelected(Icon icon) => selectedIcons.Contains(icon);

        private void SelectIcon(Icon icon)
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
            selectedIcons = new List<Icon>();
        }

        private async Task CopyToClipboard()
        {
            var iconlist = "";
            foreach (var icon in selectedIcons)
            {
                iconlist += icon.GetStaticProperty() + Environment.NewLine;
            }

            await tabService.CopyToClipboard(iconlist);
            await toastService.AddToastAsync(new TabBlazor.ToastModel { Title = $"{selectedIcons.Count} icons copied to clipboard", Options = new TabBlazor.ToastOptions { Delay = 2 } });
        }

    }

    public class Icon
    {
        public string Name { get; set; }
        public string Elements { get; set; }

        public string GetStaticProperty()
        {
            return $"public static string {Name} {"{"} get => @\"{Elements}\"; {"}"} ";
        }
    }

}
