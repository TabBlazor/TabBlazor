using System.Diagnostics.CodeAnalysis;

namespace TabBlazor
{
    public partial class NavigationItem : TablerBaseComponent, IDisposable
    {
        [CascadingParameter] Navigation Navigation { get; set; }
        [CascadingParameter] NavigationItem Parent { get; set; }

        [Parameter] public string Title { get; set; }
        [Parameter] public RenderFragment MenuIcon { get; set; }

        [Parameter] public RenderFragment SubMenu { get; set; }


        private List<NavigationItem> Children { get; set; } = new();

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (Parent != null)
            {
                Parent.AddChildItem(this);
            }
            else
            {
                Navigation?.AddChildItem(this);
            }
        }

        private bool isExpanded;
        private bool isActive;
           
        internal void AddChildItem(NavigationItem child)
        {
            Children.Add(child);
        }

        internal void RemoveChildItem(NavigationItem child)
        {
            Children.Remove(child);
        }

        private bool hasSubMenu => SubMenu != null;

        protected override string ClassNames => ClassBuilder
            .Add("nav-item")
            .Add("cursor-pointer")
            .AddIf("dropdown", hasSubMenu)
            .ToString();

        protected string LinkCss => new ClassBuilder()
            .Add("nav-link")
             .AddIf("dropdown-toggle", hasSubMenu)
            .AddIf("active", isActive) // && !hasSubMenu)
            .ToString();


        private async Task ItemClickedAsync()
        {

            await Navigation?.NavigationItemClicked(this);

            if (!hasSubMenu)
            {
                isActive = true;
            }
            else
            {
                isExpanded = !isExpanded;
                isActive = isExpanded;
            }

            SetActive(isActive);
        }

        public void SetExpanded(bool expanded)
        {
            isExpanded = expanded;
        }

        public void SetActive(bool active)
        {
            this.isActive = active;

            if (Parent != null)
            {
                if (active)
                {
                    foreach (var child in Parent.Children)
                    {
                        if (child != this)
                        {
                            child.SetActive(false);
                            child.SetExpanded(false);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            if (Parent != null)
            {
                Parent.RemoveChildItem(this);
            }
            else
            {
                Navigation?.RemoveChildItem(this);
            }
        }
    }
}
