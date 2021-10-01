using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public partial class Navbar : TablerBaseComponent
    {
        [Parameter] public bool Darkmode { get; set; }
        [Parameter] public NavbarDirection Direction { get; set; }
        protected string HtmlTag => "aside";
        protected bool isExpanded = true;

        protected override string ClassNames => ClassBuilder
              .Add("navbar navbar-expand-md")
              .AddIf("navbar-dark theme-dark", Darkmode)
              .AddIf("navbar-vertical", Direction == NavbarDirection.Vertical)
              .ToString();

        protected void ToogleExpand()
        {
            isExpanded = !isExpanded;
        }
    }

}
