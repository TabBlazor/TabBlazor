using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public partial class Navbar : TablerBaseComponent
    {
        [Parameter] public bool Darkmode { get; set; }

        protected string HtmlTag => "aside";
        protected bool isExpanded = true;

        protected override string ClassNames => ClassBuilder
              .Add("navbar navbar-vertical navbar-expand-md")
              .AddIf("navbar-dark", Darkmode)
              .ToString();

        protected void ToogleExpand()
        {
            isExpanded = !isExpanded;
        }
    }

}
