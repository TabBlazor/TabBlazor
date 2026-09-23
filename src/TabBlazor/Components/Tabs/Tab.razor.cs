using Microsoft.AspNetCore.Components.Web;
using TabBlazor.Components;

namespace TabBlazor
{
   /// <summary>A single tab page within a <c>Tabs</c> container.</summary>
   public partial class Tab : TablerBaseComponent, ITab, IDisposable
    {
        [CascadingParameter] Tabs ContainerTabSet { get; set; }
        /// <summary>The tab title. Ignored when <see cref="Header"/> is set.</summary>
        [Parameter] public string Title { get; set; }
        /// <summary>Optional custom header content, overriding <see cref="Title"/>.</summary>
        [Parameter] public RenderFragment Header { get; set; }
        /// <summary>Whether this tab is selected initially. Defaults to false. A tab selected by the URL takes precedence.</summary>
        [Parameter] public bool Active { get; set; }
        /// <summary>
        /// Permanent identifier used in the URL when <see cref="Tabs.UrlParameter"/> is set, e.g. <c>?tab=orders</c>.
        /// Must be unique within its <c>Tabs</c>. Without it, the tab is identified by its 1-based position.
        /// </summary>
        [Parameter] public string Id { get; set; }
        /// <summary>Preload mode for this tab, overriding <see cref="Tabs.Preload"/> when set.</summary>
        [Parameter] public TabPreload? Preload { get; set; }
        /// <summary>Invoked when this tab's content starts preloading, before it is shown.</summary>
        [Parameter] public EventCallback OnPreload { get; set; }
        /// <summary>Invoked after this tab has become the visible tab.</summary>
        [Parameter] public EventCallback OnActivated { get; set; }

        private CancellationTokenSource hoverIntent;

        string TitleCssClass => ContainerTabSet.ActiveTab == this ? "active" : null;

        string Href => ContainerTabSet.GetTabUrl(this);

        string TabIndex => Href == null ? "0" : null;

        bool PreventClickNavigation => Href != null && !ContainerTabSet.NavigatesThroughLink;

        Dictionary<string, object> PreloadHandlers => ContainerTabSet.GetPreloadMode(this) == TabPreload.Hover
            ? new()
            {
                ["onmouseenter"] = EventCallback.Factory.Create<MouseEventArgs>(this, StartHoverIntent),
                ["onmouseleave"] = EventCallback.Factory.Create<MouseEventArgs>(this, CancelHoverIntent),
                ["onfocus"] = EventCallback.Factory.Create<FocusEventArgs>(this, PreloadNow),
                ["onpointerdown"] = EventCallback.Factory.Create<PointerEventArgs>(this, PreloadNow)
            }
            : null;

        protected override async Task OnInitializedAsync()
        {
            ContainerTabSet.RegisterTab(this, Active);

            if (ContainerTabSet.GetPreloadMode(this) == TabPreload.Eager)
            {
                await ContainerTabSet.EagerPreloadAsync(this);
            }
        }

        public void Dispose()
        {
            CancelHoverIntent();
            ContainerTabSet.RemoveTab(this);
        }

        void ActivateOnEnter(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && Href == null)
            {
                Activate();
            }
        }

        void Activate()
        {
            CancelHoverIntent();
            ContainerTabSet.ActivateFromUser(this);
            OnClick.InvokeAsync();
        }

        async Task StartHoverIntent()
        {
            CancelHoverIntent();

            if (ContainerTabSet.EffectivePreloadDelay > 0)
            {
                hoverIntent = new CancellationTokenSource();
                try
                {
                    await Task.Delay(ContainerTabSet.EffectivePreloadDelay, hoverIntent.Token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            await PreloadNow();
        }

        void CancelHoverIntent()
        {
            hoverIntent?.Cancel();
            hoverIntent?.Dispose();
            hoverIntent = null;
        }

        Task PreloadNow() => ContainerTabSet.PreloadAsync(this);
    }
}
