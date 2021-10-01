using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TabBlazor
{
    public partial class DropdownItem : TablerBaseComponent, IDisposable
    {
        [CascadingParameter(Name = "Dropdown")] public Dropdown Dropdown { get; set; }
        [CascadingParameter(Name = "DropdownMenu")] public DropdownMenu ParentMenu { get; set; }
        [Parameter] public bool Active { get; set; }
        [Parameter] public bool Disabled { get; set; }

        [Parameter] public RenderFragment SubMenu { get; set; }
        private List<DropdownItem> subItems = new();

        private bool hasSubMenu => SubMenu != null;
        private bool subMenuVisible;

        protected override void OnInitialized()
        {
            if (hasSubMenu)
            {
                ParentMenu?.AddSubMenuItem(this);
            }

        }
     
        private void ItemClicked(MouseEventArgs e)
        {
            if (hasSubMenu)
            {
                ToogleSubMenus(e);
            }
            else if (!hasSubMenu && Dropdown.CloseOnClick)
            {
                Dropdown.Close();
            }

            OnClick.InvokeAsync(e);
        }

        protected override string ClassNames => ClassBuilder
            .Add("dropdown-item")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("active", Active)
            .AddIf("disabled", Disabled)
            .AddIf("dropdown-toggle", hasSubMenu)
            .ToString();


        public void CloseSubMenu()
        {
            subMenuVisible = false;
        }

        private void ToogleSubMenus(MouseEventArgs e)
        {
            var visible = subMenuVisible;
            ParentMenu?.CloseAllSubMenus();

            subMenuVisible = !visible;

        }

        private string GetWrapperClass()
        {
            if (hasSubMenu)
            {
                if(Dropdown.SubMenusDirection == DropdownDirection.Down)
                {
                    return "dropdown";
                }
                else
                {
                    return "dropend";
                }
                
            }
            return "";
        }

        public void Dispose()
        {
            ParentMenu?.RemoveSubMenuItem(this);

        }
    }
}