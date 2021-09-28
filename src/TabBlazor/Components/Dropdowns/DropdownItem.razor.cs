using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Linq;

namespace TabBlazor
{
    public partial class DropdownItem : TablerBaseComponent
    {
        [Parameter] public bool Active { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [CascadingParameter(Name = "DropdownMenu")] public DropdownMenu ParentMenu { get; set; }

        private List<DropdownMenu> subMenus = new();

        private bool hasSubMenus => subMenus.Any();

        protected override string ClassNames => ClassBuilder
            .Add("dropdown-item")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("active", Active)
            .AddIf("disabled", Disabled)
            .AddIf("dropdown-toggle", hasSubMenus)
            .ToString();


        public void AddSubMenu(DropdownMenu menu)
        {
            if (menu != null && !subMenus.Contains(menu))
            {
                subMenus.Add(menu);
            }
           
            StateHasChanged();
        }

        public void RemoveSubMenu(DropdownMenu menu)
        {
            if (menu != null && !subMenus.Contains(menu))
            {
                subMenus.Add(menu);
            }

            StateHasChanged();
        }

        private  void ToogleSubMenus(MouseEventArgs e)
        {
            ParentMenu?.CloseAllOtherSubMenus(this);
            foreach (var subMenu in subMenus)
            {
                subMenu.ToogleVisible();
            }
        }

        private string GetWrapperClass()
        {
            if (hasSubMenus)
            {
                return "dropend";
            }
            return "";
        }
    }
}