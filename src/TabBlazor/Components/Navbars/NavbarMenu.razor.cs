namespace TabBlazor;

public partial class NavbarMenu : TablerBaseComponent
{
    [CascadingParameter(Name = "Navbar")] private Navbar Navbar { get; set; }

    private bool IsExpanded => Navbar.IsExpanded;
    private string HtmlTag => "div";

    protected override string ClassNames => ClassBuilder
        .Add("navbar-collapse")
        .AddIf("collapse", !IsExpanded)
        .ToString();
}