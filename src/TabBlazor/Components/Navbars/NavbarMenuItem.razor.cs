using Microsoft.AspNetCore.Components;
using System;

namespace TabBlazor
{
    public partial class NavbarMenuItem : TablerBaseComponent, IDisposable
    {
        [CascadingParameter(Name = "Navbar")] Navbar Navbar { get; set; }
        [CascadingParameter(Name = "Parent")] NavbarMenuItem ParentMenuItem { get; set; }

        [Parameter] public string Href { get; set; }
        [Parameter] public string Text { get; set; }
        [Parameter] public RenderFragment MenuItemIcon { get; set; }
        [Parameter] public RenderFragment SubMenu { get; set; }
        [Parameter] public bool Expanded { get; set; }
        [Parameter] public bool Expandable { get; set; } = true;

        public bool IsTopMenuItem => ParentMenuItem == null;

        protected string HtmlTag => "li";
        protected bool isExpanded;
        protected bool IsDropdown => SubMenu != null && Expandable;

        protected bool isSubMenu => ParentMenuItem != null;

        protected override void OnInitialized()
        {
            isExpanded = Expanded;
            Navbar?.AddNavbarMenuItem(this);
        }

        private bool isDropEnd => Navbar.Direction == NavbarDirection.Horizontal && ParentMenuItem?.IsDropdown == true;

        protected override string ClassNames => ClassBuilder
            .Add("nav-item")
            .Add("cursor-pointer")
            .AddIf("dropdown", IsDropdown && !isDropEnd)
            .AddIf("dropend", IsDropdown && isDropEnd)
            .ToString();

        public void CloseDropdown()
        {
            isExpanded = false;
        }

        public void ToogleDropdown()
        {
            var expand = !isExpanded;

            if (expand && IsTopMenuItem)
            {
                Navbar.CloseAll();
            }

            isExpanded = expand;

        }

        public void Dispose()
        {
            Navbar?.RemoveNavbarMenuItem(this);
        }
    }
}

