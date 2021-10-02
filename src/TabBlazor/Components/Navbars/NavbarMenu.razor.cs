
using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public partial class NavbarMenu : TablerBaseComponent
    {
        [CascadingParameter(Name = "Navbar")] Navbar Navbar { get; set; }

        private bool isExpanded => Navbar.IsExpanded;
        protected string HtmlTag => "div";
        protected override string ClassNames => ClassBuilder
              .Add("navbar-collapse")
              .AddIf("collapse", !isExpanded)
              .ToString();



        public void ToogleExpanded()
        {
           
            Navbar.ToogleExpand();
        }

        private string menuCollapse => isExpanded ? "" : "collapse";

    }
}
