using TabBlazor.Components;

namespace TabBlazor
{
   public partial class Tab : TablerBaseComponent, ITab
    {
        [CascadingParameter] Tabs ContainerTabSet { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public RenderFragment Header { get; set; }
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
