using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TabBlazor
{
    public partial class DropdownMenu : TablerBaseComponent, IDisposable
    {
        //[Parameter] public int Columns { get; set; } = 1;
        [Parameter] public bool Arrow { get; set; } = false;
        [CascadingParameter(Name = "Dropdown")] public Dropdown Dropdown { get; set; }
        [CascadingParameter(Name = "DropdownItem")] public DropdownItem ParentItem { get; set; }
        [CascadingParameter(Name = "DropdownMenu")] public DropdownMenu ParentMenu { get; set; }

        private List<DropdownMenu> subMenus = new();

        private bool isSubMenu => ParentItem != null;
        private bool isVisible = true;
        protected override void OnInitialized()
        {
    
            ParentItem?.AddSubMenu(this);
            ParentMenu?.AddSubMenu(this);
            isVisible = !isSubMenu;
        }

        public void ToogleVisible()
        {
            isVisible = !isVisible;
        }
        public void Close()
        {
            isVisible = false;
        }

        private void MenuClicked(MouseEventArgs e)
        {
            if (isSubMenu)
            {
                Dropdown.Close();
            }
            OnClick.InvokeAsync(e);
        }

        public void CloseAllOtherSubMenus(DropdownItem parent)
        {
            foreach(var subMenu in subMenus.Where(e=> e.ParentItem != parent))
            {
                subMenu.Close();
            }
            StateHasChanged();
        }

        public void AddSubMenu(DropdownMenu menu)
        {
            if (menu != null && !subMenus.Contains(menu))
            {
                subMenus.Add(menu);
            }

        }

        public void RemoveSubMenu(DropdownMenu menu)
        {
            if (menu != null && !subMenus.Contains(menu))
            {
                subMenus.Add(menu);
            }

        }

        public void Dispose()
        {
            ParentItem?.RemoveSubMenu(this);
            ParentMenu?.RemoveSubMenu(this);
        }

        protected override string ClassNames => ClassBuilder
            .Add("dropdown-menu")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("show", isVisible)
            .AddIf($"dropdown-menu-arrow", Arrow)
            //.AddIf($"dropdown-menu-columns dropdown-menu-columns-{Columns}", Columns > 1)
            .ToString();
    }
}