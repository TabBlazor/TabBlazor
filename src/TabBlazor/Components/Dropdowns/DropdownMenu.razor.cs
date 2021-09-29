using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TabBlazor
{
    public partial class DropdownMenu : TablerBaseComponent
    {
        [Parameter] public bool Arrow { get; set; } = false;
      
        private List<DropdownItem> subMenuItems = new();

        protected override string ClassNames => ClassBuilder
           .Add("dropdown-menu")
           .Add(BackgroundColor.GetColorClass("bg"))
           .Add(TextColor.GetColorClass("text"))
           .AddIf("show", true)
           .AddIf($"dropdown-menu-arrow", Arrow)
           .ToString();

        public void CloseAllSubMenus()
        {
            foreach (var item in subMenuItems)
            {
                item.CloseSubMenu();
            }
            StateHasChanged();
        }

        public void AddSubMenuItem(DropdownItem item)
        {
            if (item != null && !subMenuItems.Contains(item))
            {
                subMenuItems.Add(item);
            }
        }

        public void RemoveSubMenuItem(DropdownItem item)
        {
            if (item != null && subMenuItems.Contains(item))
            {
                subMenuItems.Remove(item);
            }
        }
     
    }
}