using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public partial class NavbarMenuItem : TablerBaseComponent
    {
        [CascadingParameter(Name = "Parent")] NavbarMenuItem ParentMenuItem { get; set; }
        [Parameter] public string Href { get; set; }
        [Parameter] public string Text { get; set; }
        [Parameter] public RenderFragment MenuItemIcon { get; set; }
        [Parameter] public RenderFragment SubMenu { get; set; }
        [Parameter] public bool Expanded { get; set; }
        [Parameter] public bool Expandable { get; set; } = true;

        protected string HtmlTag => "li";
        protected bool isExpanded;
        protected bool IsDropdown => SubMenu != null && Expandable;

        protected bool isSubMenu => ParentMenuItem != null;

        protected override void OnInitialized()
        {
            isExpanded = Expanded;
        }

        protected override string ClassNames => ClassBuilder
            .Add("nav-item")
            .Add("cursor-pointer")
            .AddIf("dropdown", IsDropdown)
            .ToString();


        protected void ToogleDropdown()
        {
            isExpanded = !isExpanded;
        }
    }
}

