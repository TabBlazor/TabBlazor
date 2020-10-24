using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tabler.Icons;

namespace Tabler.Docs.Components.Icons
{
   public partial class IconsList : ComponentBase
    {
      
        private List<TablerIconType> icons = Enum.GetValues(typeof(TablerIconType)).Cast<TablerIconType>().ToList();
        private List<TablerIconType> filteredIcons = Enum.GetValues(typeof(TablerIconType)).Cast<TablerIconType>().ToList();
        private int size = 24;
        private double strokeWidth = 2;
        private string searchText;
        private string color;

  
        private void SearchIcons(ChangeEventArgs e)
        {

            searchText = e.Value.ToString();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredIcons = icons;
            }
            else
            {
                filteredIcons = icons.Where(x => x.GetIconName().Contains(searchText, StringComparison.InvariantCulture)).ToList();
            }

        }

    }
}
