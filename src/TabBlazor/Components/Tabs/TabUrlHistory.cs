namespace TabBlazor
{
    /// <summary>Controls how selecting a tab updates browser history when <see cref="Tabs.UrlParameter"/> is set.</summary>
    public enum TabUrlHistory
    {
        /// <summary>Replace the current history entry, so Back leaves the page.</summary>
        Replace,
        /// <summary>Add a history entry per tab selection, so Back returns to the previous tab.</summary>
        Push
    }
}
