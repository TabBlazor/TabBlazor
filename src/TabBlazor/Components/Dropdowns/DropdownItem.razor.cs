using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TabBlazor
{
    /// <summary>A single selectable item within a <see cref="DropdownMenu"/>, optionally with a sub-menu.</summary>
    public partial class DropdownItem : TablerBaseComponent, IDisposable
    {
        /// <summary>The owning dropdown, supplied via cascading parameter.</summary>
        [CascadingParameter(Name = "Dropdown")] public Dropdown Dropdown { get; set; }
        /// <summary>The parent menu, supplied via cascading parameter.</summary>
        [CascadingParameter(Name = "DropdownMenu")] public DropdownMenu ParentMenu { get; set; }
        /// <summary>When true, marks the item as active. Defaults to false.</summary>
        [Parameter] public bool Active { get; set; }
        /// <summary>When true, the item is disabled. Defaults to false.</summary>
        [Parameter] public bool Disabled { get; set; }
        /// <summary>When true, highlights the item. Defaults to false.</summary>
        [Parameter] public bool Highlight { get; set; }

        /// <summary>Optional nested sub-menu content shown on hover/click.</summary>
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
            .AddIf("highlight", Highlight)
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
