using System.Diagnostics.CodeAnalysis;

namespace TabBlazor
{
    /// <summary>An entry within a <see cref="Navigation"/>, optionally containing a nested sub-menu.</summary>
    public partial class NavigationItem : NavigationBase
    {

        /// <summary>The item label text.</summary>
        [Parameter] public string Title { get; set; }

        /// <summary>Optional icon content shown before the title.</summary>
        [Parameter] public RenderFragment MenuIcon { get; set; }
        /// <summary>Optional nested sub-menu content.</summary>
        [Parameter] public RenderFragment SubMenu { get; set; }

        /// <summary>Arbitrary data associated with the item.</summary>
        [Parameter] public object Data { get; set; }


        private bool hasSubMenu => SubMenu != null;

        protected override string ClassNames => ClassBuilder
            .Add("nav-item")
            .Add("cursor-pointer")
            .AddIf("dropdown", hasSubMenu && IsExpanded)
            .ToString();

        protected string LinkCss => new ClassBuilder()
            .Add("nav-link")
             .AddIf("dropdown-toggle", hasSubMenu)
            .AddIf("active", IsActive && !Disabled)
            .AddIf("active", Disabled)
            .ToString();

      
        private void MouseEnter()
        {
            IsExpanded = true;
        }

        private void MouseLeave()
        {
            IsExpanded = false;
        }


        private void ItemClicked()
        {
            bool isActive;

            if (!ExpandClick) {
                isActive = true;
                CollapseAll();
            }
            else if (!hasSubMenu)
            {
                isActive = !IsActive;
            }
            else
            {
                IsExpanded = !IsExpanded;
                isActive = IsExpanded;
            }

            SetActive(isActive);

            if (isActive)
            {
                ChildSelected(this);
            }

        }


    }
}
