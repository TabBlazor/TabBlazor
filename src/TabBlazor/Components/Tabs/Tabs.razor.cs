using TabBlazor.Components;

namespace TabBlazor
{
    public partial class Tabs : TablerBaseComponent
    {
        /// <summary>Default preload mode for all tabs. A tab can override it with <see cref="Tab.Preload"/>. Defaults to <see cref="TabPreload.None"/>.</summary>
        [Parameter] public TabPreload Preload { get; set; } = TabPreload.None;
        /// <summary>Milliseconds the pointer must rest on a tab header before hover preloading starts. Defaults to 0.</summary>
        [Parameter] public int PreloadDelay { get; set; }
        /// <summary>Whether visited tabs stay rendered (hidden) after another tab is activated. Defaults to false.</summary>
        [Parameter] public bool KeepAlive { get; set; }

        public ITab ActiveTab { get; private set; }

        private readonly List<ITab> tabs = new();
        private readonly HashSet<ITab> renderedTabs = new();
        private ITab pendingActivation;

        public void AddTab(ITab tab)
        {
            if (!tabs.Contains(tab))
            {
                tabs.Add(tab);
            }

            if (ActiveTab == null)
            {
                SetActivateTab(tab);
            }
        }

        public void RemoveTab(ITab tab)
        {
            tabs.Remove(tab);
            var wasRendered = renderedTabs.Remove(tab);

            if (pendingActivation == tab)
            {
                pendingActivation = null;
            }

            if (ActiveTab == tab)
            {
                ActiveTab = null;
                SetActivateTab(tabs.FirstOrDefault());
            }
            else if (wasRendered)
            {
                StateHasChanged();
            }
        }

        public void SetActivateTab(ITab tab)
        {
            if (tab == null)
            {
                ActiveTab = null;
                StateHasChanged();
                return;
            }

            if (ActiveTab == tab)
            {
                return;
            }

            var previousTab = ActiveTab;
            ActiveTab = tab;
            renderedTabs.Add(tab);

            if (previousTab != null && !KeepAlive && GetPreloadMode(previousTab) != TabPreload.Eager)
            {
                renderedTabs.Remove(previousTab);
            }

            pendingActivation = tab;
            StateHasChanged();
        }

        internal TabPreload GetPreloadMode(ITab tab) => (tab as Tab)?.Preload ?? Preload;

        internal Task PreloadAsync(ITab tab) =>
            renderedTabs.Contains(tab) ? Task.CompletedTask : StartPreloadAsync(tab);

        internal Task EagerPreloadAsync(ITab tab) => StartPreloadAsync(tab);

        private async Task StartPreloadAsync(ITab tab)
        {
            if (!tabs.Contains(tab))
            {
                return;
            }

            var preloading = tab is Tab preloadedTab ? preloadedTab.OnPreload.InvokeAsync() : Task.CompletedTask;
            if (renderedTabs.Add(tab))
            {
                StateHasChanged();
            }
            await preloading;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (pendingActivation is Tab activatedTab)
            {
                pendingActivation = null;
                await activatedTab.OnActivated.InvokeAsync();
            }
        }
    }
}
