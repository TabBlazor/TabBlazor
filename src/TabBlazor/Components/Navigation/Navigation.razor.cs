using Microsoft.AspNetCore.Components.Web;
using System.Reflection;

namespace TabBlazor
{
    public partial class Navigation : TablerBaseComponent
    {
        [Parameter] public bool Bordered { get; set; }

        [Parameter] public EventCallback<NavigationItem> OnItemClicked { get; set; }

        public NavigationItem SelectedItem { get; set; }

        //  public bool CollapseAll { get; set; }


        private List<NavigationItem> Children { get; set; } = new();

        internal void AddChildItem(NavigationItem child)
        {
            Children.Add(child);
        }

        internal void RemoveChildItem(NavigationItem child)
        {
            Children.Remove(child);
        }

        protected override string ClassNames => ClassBuilder
             .Add("nav")
                .AddIf("nav-bordered", Bordered)
             .ToString();

        internal async Task NavigationItemClicked(NavigationItem item)
        {
            SelectedItem = item;
            await OnItemClicked.InvokeAsync(item);
        }

        private async Task OnClickOutside()
        {
            await NavigationItemClicked(null);
            StateHasChanged();
        }

    }
}