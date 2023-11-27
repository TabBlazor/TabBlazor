using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
    public abstract class NavigationBase : TablerBaseComponent, IDisposable
    {
        [CascadingParameter] public NavigationBase Parent { get; set; }


        internal List<NavigationBase> Children { get; set; } = new();

        public NavigationItem SelectedItem { get; set; }

        internal bool IsExpanded;
        internal bool IsActive;

        internal bool ExpandClick;

        public bool Disabled { get; set; }  


        protected override void OnInitialized()
        {
            base.OnInitialized();
            Parent?.AddChildItem(this);
        }


        public void AddChildItem(NavigationBase child)
        {
            child.ExpandClick = ExpandClick;
            Children.Add(child);
        }

        public void RemoveChildItem(NavigationBase child)
        {
            Children.Remove(child);
        }

        public virtual void ChildSelected(NavigationItem child)
        {
            SelectedItem = child;
            SetActive(true);
            Parent?.ChildSelected(child);
        }

        public void SetExpanded(bool expanded)
        {
            IsExpanded = expanded;
        }

        public void CollapseAll() { 
        
            if (Parent != null)
            {
                Parent.CollapseAll();
            }
            else
            {
                foreach (var child in Children) {

                    child.SetExpanded(false);
                }
            }

        }


        public void SetActive(bool active)
        {
            IsActive = active;

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

            StateHasChanged();

        }

        public void Dispose()
        {
            Parent?.RemoveChildItem(this);
        }
    }
}
