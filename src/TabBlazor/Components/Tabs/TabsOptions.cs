namespace TabBlazor
{
    /// <summary>Global defaults for every <c>Tabs</c> component. Parameters set on a <c>Tabs</c> or <c>Tab</c> take precedence.</summary>
    public class TabsOptions
    {
        /// <summary>Default preload mode. Defaults to <see cref="TabPreload.None"/>.</summary>
        public TabPreload Preload { get; set; } = TabPreload.None;

        /// <summary>Default milliseconds the pointer must rest on a tab header before hover preloading starts. Defaults to 0.</summary>
        public int PreloadDelay { get; set; }

        /// <summary>Whether visited tabs stay rendered (hidden) after another tab is activated. Defaults to false.</summary>
        public bool KeepAlive { get; set; }

        /// <summary>How selecting a tab updates browser history when URL sync is enabled. Defaults to <see cref="TabUrlHistory.Replace"/>.</summary>
        public TabUrlHistory UrlHistory { get; set; } = TabUrlHistory.Replace;
    }
}
