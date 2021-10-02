using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public partial class Navbar : TablerBaseComponent
    {
        [Parameter] public bool Darkmode { get; set; }
        [Parameter] public NavbarDirection Direction { get; set; }
        protected string HtmlTag => "aside";
        public bool IsExpanded = true;

        protected override string ClassNames => ClassBuilder
              .Add("navbar navbar-expand-md")
              .AddIf("navbar-dark", Darkmode)
            .AddIf("navbar-light", !Darkmode)
              .AddIf("navbar-vertical", Direction == NavbarDirection.Vertical)
              .ToString();

        public void ToogleExpand()
        {
            IsExpanded = !IsExpanded;
        }

        
    }

}
