namespace Tabler.Components
{
    public partial class TablerTabs : TablerBaseComponent
    {
        public ITablerTab ActiveTab { get; private set; }

        public void AddTab(ITablerTab tab)
        {
            if (ActiveTab == null)
            {
                SetActivateTab(tab);
            }
        }

        public void RemoveTab(ITablerTab tab)
        {
            if (ActiveTab == tab)
            {
                SetActivateTab(null);
            }
        }

        public void SetActivateTab(ITablerTab tab)
        {
            if (tab == null)
            {
                return;
            }

            if (ActiveTab != tab)
            {
                ActiveTab = tab;
                StateHasChanged();
            }
        }
    }
}
