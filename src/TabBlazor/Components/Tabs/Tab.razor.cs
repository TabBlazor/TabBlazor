using TabBlazor.Components;

namespace TabBlazor
{
   /// <summary>A single tab page within a <c>Tabs</c> container.</summary>
   public partial class Tab : TablerBaseComponent, ITab
    {
        [CascadingParameter] Tabs ContainerTabSet { get; set; }
        /// <summary>The tab title. Ignored when <see cref="Header"/> is set.</summary>
        [Parameter] public string Title { get; set; }
        /// <summary>Optional custom header content, overriding <see cref="Title"/>.</summary>
        [Parameter] public RenderFragment Header { get; set; }
        /// <summary>Whether this tab is selected initially. Defaults to false.</summary>
        [Parameter] public bool Active { get; set; }

        string TitleCssClass => ContainerTabSet.ActiveTab == this ? "active" : null;

        protected override void OnInitialized()
        {
            ContainerTabSet.AddTab(this);
            if (Active)
            {
                ContainerTabSet.SetActivateTab(this);
            }
        }

        public void Dispose()
        {
            ContainerTabSet.RemoveTab(this);
        }

        void Activate()
        {
            ContainerTabSet.SetActivateTab(this);
            OnClick.InvokeAsync();
        }
    }
}
