using System.Diagnostics.CodeAnalysis;

namespace TabBlazor
{
    public partial class NavigationItem : NavigationBase
    {

        [Parameter] public string Title { get; set; }
     
        [Parameter] public RenderFragment MenuIcon { get; set; }
        [Parameter] public RenderFragment SubMenu { get; set; }

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
