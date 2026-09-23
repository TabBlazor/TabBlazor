using System.Web;
using Microsoft.AspNetCore.Components.Routing;
using TabBlazor.Components;

namespace TabBlazor
{
    public partial class Tabs : TablerBaseComponent, IDisposable
    {
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private TabsUrlParameterRegistry UrlParameterRegistry { get; set; }

        /// <summary>Preload mode for all tabs. A tab can override it with <see cref="Tab.Preload"/>. Defaults to <see cref="TabsOptions.Preload"/>.</summary>
        [Parameter] public TabPreload? Preload { get; set; }
        /// <summary>Milliseconds the pointer must rest on a tab header before hover preloading starts. Defaults to <see cref="TabsOptions.PreloadDelay"/>.</summary>
        [Parameter] public int? PreloadDelay { get; set; }
        /// <summary>Whether visited tabs stay rendered (hidden) after another tab is activated. Defaults to <see cref="TabsOptions.KeepAlive"/>.</summary>
        [Parameter] public bool? KeepAlive { get; set; }
        /// <summary>
        /// Query string parameter that stores the selected tab, enabling deep links such as <c>?tab=orders</c>.
        /// Tabs are identified by <see cref="Tab.Id"/>, or by their 1-based position when no Id is set. Null disables URL sync.
        /// </summary>
        [Parameter] public string UrlParameter { get; set; }
        /// <summary>How selecting a tab updates browser history when <see cref="UrlParameter"/> is set. Defaults to <see cref="TabsOptions.UrlHistory"/>.</summary>
        [Parameter] public TabUrlHistory? UrlHistory { get; set; }

        public ITab ActiveTab { get; private set; }

        private enum UrlMatch { None, Position, Id }

        private readonly List<ITab> tabs = new();
        private readonly HashSet<ITab> renderedTabs = new();
        private ITab pendingActivation;
        private ITab activeMarkedTab;
        private string requestedUrlValue;
        private UrlMatch urlMatch;
        private string registeredUrlParameter;

        private TabsOptions TabsDefaults => Options.CurrentValue.Tabs;
        private bool EffectiveKeepAlive => KeepAlive ?? TabsDefaults.KeepAlive;
        private TabUrlHistory EffectiveUrlHistory => UrlHistory ?? TabsDefaults.UrlHistory;
        internal int EffectivePreloadDelay => PreloadDelay ?? TabsDefaults.PreloadDelay;
        private bool UrlSyncEnabled => !string.IsNullOrEmpty(UrlParameter);
        private ITab DefaultTab => activeMarkedTab ?? tabs.FirstOrDefault();

        protected override void OnInitialized()
        {
            requestedUrlValue = ReadUrlValue(NavigationManager.Uri);
            NavigationManager.LocationChanged += LocationChanged;
        }

        public void AddTab(ITab tab) => RegisterTab(tab, false);

        internal void RegisterTab(ITab tab, bool markedActive)
        {
            if (!tabs.Contains(tab))
            {
                EnsureUniqueId(tab);
                tabs.Add(tab);
            }

            if (markedActive)
            {
                activeMarkedTab = tab;
            }

            var match = MatchUrlValue(tab, requestedUrlValue);
            if (match > urlMatch)
            {
                urlMatch = match;
                SetActivateTab(tab);
                return;
            }

            if (urlMatch == UrlMatch.None && (markedActive || ActiveTab == null))
            {
                SetActivateTab(tab);
            }
        }

        public void RemoveTab(ITab tab)
        {
            tabs.Remove(tab);
            var wasRendered = renderedTabs.Remove(tab);

            if (activeMarkedTab == tab)
            {
                activeMarkedTab = null;
            }

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

            if (previousTab != null && !EffectiveKeepAlive && GetPreloadMode(previousTab) != TabPreload.Eager)
            {
                renderedTabs.Remove(previousTab);
            }

            pendingActivation = tab;
            StateHasChanged();
        }

        internal void ActivateFromUser(ITab tab)
        {
            SetActivateTab(tab);

            if (UrlSyncEnabled && EffectiveUrlHistory == TabUrlHistory.Replace)
            {
                NavigationManager.NavigateTo(GetTabUrl(tab), replace: true);
            }
        }

        internal string GetTabUrl(ITab tab)
        {
            if (!UrlSyncEnabled)
            {
                return null;
            }

            var value = tab == DefaultTab ? null : GetUrlValue(tab);
            var uri = NavigationManager.Uri;

            if (value != null && HasRepeatedUrlParameter(uri))
            {
                uri = NavigationManager.GetUriWithQueryParameter(UrlParameter, (string)null);
            }

            return NavigationManager.GetUriWithQueryParameters(uri, new Dictionary<string, object> { [UrlParameter] = value });
        }

        internal bool NavigatesThroughLink => UrlSyncEnabled && EffectiveUrlHistory == TabUrlHistory.Push;

        internal TabPreload GetPreloadMode(ITab tab) => (tab as Tab)?.Preload ?? Preload ?? TabsDefaults.Preload;

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
            RegisterUrlParameter();

            if (pendingActivation is Tab activatedTab)
            {
                pendingActivation = null;
                await activatedTab.OnActivated.InvokeAsync();
            }
        }

        private void LocationChanged(object sender, LocationChangedEventArgs e)
        {
            if (!UrlSyncEnabled)
            {
                return;
            }

            _ = InvokeAsync(() =>
            {
                requestedUrlValue = ReadUrlValue(e.Location);
                var (tab, match) = FindTab(requestedUrlValue);
                urlMatch = match;

                if (tab != null)
                {
                    SetActivateTab(tab);
                }
                else if (requestedUrlValue == null && DefaultTab != null)
                {
                    SetActivateTab(DefaultTab);
                }

                StateHasChanged();
            });
        }

        private (ITab Tab, UrlMatch Match) FindTab(string urlValue)
        {
            var idMatch = tabs.FirstOrDefault(t => MatchUrlValue(t, urlValue) == UrlMatch.Id);
            if (idMatch != null)
            {
                return (idMatch, UrlMatch.Id);
            }

            var positionMatch = tabs.FirstOrDefault(t => MatchUrlValue(t, urlValue) == UrlMatch.Position);
            return positionMatch != null ? (positionMatch, UrlMatch.Position) : (null, UrlMatch.None);
        }

        private UrlMatch MatchUrlValue(ITab tab, string urlValue)
        {
            if (urlValue == null)
            {
                return UrlMatch.None;
            }

            if (tab is Tab { Id: not null } identifiedTab && string.Equals(identifiedTab.Id, urlValue, StringComparison.OrdinalIgnoreCase))
            {
                return UrlMatch.Id;
            }

            return int.TryParse(urlValue, out var position) && position == tabs.IndexOf(tab) + 1
                ? UrlMatch.Position
                : UrlMatch.None;
        }

        private string GetUrlValue(ITab tab) =>
            (tab as Tab)?.Id ?? (tabs.IndexOf(tab) + 1).ToString();

        private string ReadUrlValue(string uri)
        {
            if (!UrlSyncEnabled)
            {
                return null;
            }

            var value = HttpUtility.ParseQueryString(new Uri(uri).Query).GetValues(UrlParameter)?.FirstOrDefault();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private void RegisterUrlParameter()
        {
            var urlParameter = UrlSyncEnabled ? UrlParameter : null;
            if (string.Equals(urlParameter, registeredUrlParameter, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            UrlParameterRegistry.Unregister(registeredUrlParameter, this);
            registeredUrlParameter = null;

            if (urlParameter != null)
            {
                UrlParameterRegistry.Register(urlParameter, this);
                registeredUrlParameter = urlParameter;
            }
        }

        private bool HasRepeatedUrlParameter(string uri) =>
            HttpUtility.ParseQueryString(new Uri(uri).Query).GetValues(UrlParameter)?.Length > 1;

        private void EnsureUniqueId(ITab tab)
        {
            if (tab is Tab { Id: not null } newTab &&
                tabs.OfType<Tab>().Any(t => string.Equals(t.Id, newTab.Id, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Tabs already contains a tab with Id '{newTab.Id}'. Tab ids must be unique within a Tabs component.");
            }
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= LocationChanged;
            UrlParameterRegistry.Unregister(registeredUrlParameter, this);
        }
    }
}
