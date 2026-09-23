namespace TabBlazor
{
    /// <summary>Controls when a tab's content is rendered ahead of being shown.</summary>
    public enum TabPreload
    {
        /// <summary>Content is rendered only when the tab is activated.</summary>
        None,
        /// <summary>Content is rendered hidden when the tab header is hovered, focused or pressed.</summary>
        Hover,
        /// <summary>Content is rendered hidden as soon as the tab is added.</summary>
        Eager
    }
}
