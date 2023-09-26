using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;

namespace TabBlazor
{
    public partial class NavbarMenuItem : TablerBaseComponent, IDisposable
    {
        [CascadingParameter(Name = "Navbar")] Navbar Navbar { get; set; }
        [CascadingParameter(Name = "Parent")] NavbarMenuItem ParentMenuItem { get; set; }

        [Inject] private NavigationManager NavigationManager { get; set; }

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

            NavigationManager.LocationChanged += LocationChanged;

        }

        private void LocationChanged(object sender, LocationChangedEventArgs e)
        {
            StateHasChanged();
        }

        private bool IsActive()
        {
            if (Href == null) { return false; }

            if (Navbar.NavLinkMatch == null) { return false; }

            var navLinkMatch = (NavLinkMatch)Navbar.NavLinkMatch;

            var relativePath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).ToLower();
            return navLinkMatch == NavLinkMatch.All ? relativePath == Href.ToLower() : relativePath.StartsWith(Href.ToLower());
        }


        private bool NavbarIsHorizontalAndDark => Navbar?.Background == NavbarBackground.Dark && Navbar?.Direction == NavbarDirection.Horizontal;

        private bool isDropEnd => Navbar.Direction == NavbarDirection.Horizontal && ParentMenuItem?.IsDropdown == true;

        protected override string ClassNames => ClassBuilder
            .Add("nav-item")
            .Add("cursor-pointer")
            .AddIf("dropdown", IsDropdown && !isDropEnd)
            .AddIf("dropend", IsDropdown && isDropEnd)
            .AddIf("active", IsActive())
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
           
            if (NavigationManager != null)
            {
                NavigationManager.LocationChanged -= LocationChanged;
            }

        }
    }
}

