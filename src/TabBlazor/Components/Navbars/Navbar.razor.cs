using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TabBlazor
{
    public partial class Navbar : TablerBaseComponent, IDisposable
    {
        [Inject] private NavigationManager navigationManager { get; set; }
        [Parameter] public NavbarBackground Background { get; set; }
        [Parameter] public NavbarDirection Direction { get; set; }
        protected string HtmlTag => "div";
        public bool IsExpanded = true;

        private List<NavbarMenuItem> navbarItems = new();

        protected override void OnInitialized()
        {
            navigationManager.LocationChanged += LocationChanged;
            base.OnInitialized();
        }

        private void LocationChanged(object sender, LocationChangedEventArgs e)
        {
            if (Direction == NavbarDirection.Horizontal)
            {
                CloseAll();
            }
        }

        protected override string ClassNames => ClassBuilder
              .Add("navbar navbar-expand-md")
              .AddIf("navbar-dark", Background == NavbarBackground.Dark)
              .AddIf("navbar-light", Background == NavbarBackground.Light)
             .AddIf("navbar-transparent", Background == NavbarBackground.Transparent)
              .AddIf("navbar-vertical", Direction == NavbarDirection.Vertical)
              .ToString();

        public void ToogleExpand()
        {
            IsExpanded = !IsExpanded;
        }

        public void CloseAll()
        {
            foreach (var item in navbarItems.Where(e => e.IsTopMenuItem))
            {
                item.CloseDropdown();
            }

            StateHasChanged();
        }

        public void AddNavbarMenuItem(NavbarMenuItem item)
        {
            if (!navbarItems.Contains(item))
            {
                navbarItems.Add(item);
            }
        }

        public void RemoveNavbarMenuItem(NavbarMenuItem item)
        {
            if (navbarItems.Contains(item))
            {
                navbarItems.Remove(item);
            }
        }

        public void Dispose()
        {
            navigationManager.LocationChanged -= LocationChanged;
        }
    }

}
