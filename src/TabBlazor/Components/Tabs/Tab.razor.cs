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
        /// <summary>Whether this tab is selected initially. Defaults to false.</summary>
        [Parameter] public bool Active { get; set; }
        /// <summary>Preload mode for this tab, overriding <see cref="Tabs.Preload"/> when set.</summary>
        [Parameter] public TabPreload? Preload { get; set; }
        /// <summary>Invoked when this tab's content starts preloading, before it is shown.</summary>
        [Parameter] public EventCallback OnPreload { get; set; }
        /// <summary>Invoked after this tab has become the visible tab.</summary>
        [Parameter] public EventCallback OnActivated { get; set; }

        private CancellationTokenSource hoverIntent;

        string TitleCssClass => ContainerTabSet.ActiveTab == this ? "active" : null;

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
            ContainerTabSet.AddTab(this);
            if (Active)
            {
                ContainerTabSet.SetActivateTab(this);
            }

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
            if (e.Key == "Enter")
            {
                Activate();
            }
        }

        void Activate()
        {
            CancelHoverIntent();
            ContainerTabSet.SetActivateTab(this);
            OnClick.InvokeAsync();
        }

        async Task StartHoverIntent()
        {
            CancelHoverIntent();

            if (ContainerTabSet.PreloadDelay > 0)
            {
                hoverIntent = new CancellationTokenSource();
                try
                {
                    await Task.Delay(ContainerTabSet.PreloadDelay, hoverIntent.Token);
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
