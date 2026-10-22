namespace TabBlazor
{
    /// <summary>Where the tab headers of a <c>Tabs</c> are placed on small screens (below the <c>md</c> breakpoint).</summary>
    public enum TabsMobilePlacement
    {
        /// <summary>Headers stay above the content in a single row that scrolls sideways.</summary>
        Top,
        /// <summary>
        /// Headers become a bar fixed to the bottom of the screen, within thumb reach. Use it for one page-level
        /// <c>Tabs</c> that fills the page, and give each <c>Tab</c> an <see cref="Tab.Icon"/>.
        /// </summary>
        Bottom
    }
}
