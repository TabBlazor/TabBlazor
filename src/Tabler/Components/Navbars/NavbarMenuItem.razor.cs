using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public partial class NavbarMenuItem : TablerBaseComponent
    {
        [CascadingParameter(Name = "Parent")] NavbarMenuItem ParentMenuItem { get; set; }
        [Parameter] public string Href { get; set; }
        [Parameter] public string Text { get; set; }
        [Parameter] public RenderFragment MenuItemIcon { get; set; }
        [Parameter] public RenderFragment SubMenu { get; set; }
        [Parameter] public bool Expanded { get; set; }

        protected string HtmlTag => "li";
        protected bool isExpanded;
        protected bool IsDropdown => SubMenu != null;
        protected bool isSubMenu => ParentMenuItem != null;

        protected override void OnInitialized()
        {
            isExpanded = Expanded;
        }

        protected override string ClassNames => ClassBuilder
            .Add("nav-item")
            .Add("clickable")
            .AddIf("dropdown", IsDropdown)
            .ToString();


        protected void ToogleDropdown()
        {
            isExpanded = !isExpanded;
        }
    }
}

