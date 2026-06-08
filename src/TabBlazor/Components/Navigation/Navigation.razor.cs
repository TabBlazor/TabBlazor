

namespace TabBlazor
{
    /// <summary>The root container for a tree of <see cref="NavigationItem"/> entries (e.g. a sidebar menu).</summary>
    public partial class Navigation : NavigationBase
    {
        /// <summary>When true, renders bordered navigation styling. Defaults to false.</summary>
        [Parameter] public bool Bordered { get; set; }

        /// <summary>Raised when a navigation item is clicked.</summary>
        [Parameter] public EventCallback<NavigationItem> OnItemClicked { get; set; }

        /// <summary>When true, clicking an item expands its sub-menu rather than navigating. Defaults to false.</summary>
        [Parameter] public bool ExpandOnClick { get; set; }

      

        protected override void OnInitialized()
        {
            ExpandClick = ExpandOnClick;

            base.OnInitialized();
        }

        public override void ChildSelected(NavigationItem child)
        {
            _ = NavigationItemClicked(child);
        }

        protected override string ClassNames => this.ClassBuilder
             .Add("nav")
                .AddIf("nav-bordered", Bordered)
             .ToString();

        internal async Task NavigationItemClicked(NavigationItem item)
        {
            SelectedItem = item;
            await OnItemClicked.InvokeAsync(item);
        }

        private void OnClickOutside()
        {

            foreach (var child in Children)
            {
                child.SetExpanded(false);
            }
        }

    }
}