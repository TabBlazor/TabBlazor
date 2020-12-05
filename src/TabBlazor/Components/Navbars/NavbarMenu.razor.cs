
namespace TabBlazor
{
    public partial class NavbarMenu : TablerBaseComponent
    {
        private bool isExpanded = true;
        protected string HtmlTag => "div";
        protected override string ClassNames => ClassBuilder
              .Add("navbar-collapse")
              .AddIf("collapse", !isExpanded)
              .ToString();



        public void ToogleExpanded()
        {
            isExpanded = !isExpanded;
        }

        private string menuCollapse => isExpanded ? "" : "collapse";

    }
}
