using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tabler.Docs.Components.Icons
{
   public partial class IconsList : ComponentBase
    {

        private List<Icon> icons = new List<Icon>();
        private List<Icon> filteredIcons = new List<Icon>();
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
    }

    public class Icon
    {
        public string Name { get; set; }
        public string Elements { get; set; }
    }

}
