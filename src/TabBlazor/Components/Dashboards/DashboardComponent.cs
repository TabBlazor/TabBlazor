

namespace TabBlazor.Dashboards
{
    public class DashboardComponent<TItem> :  ComponentBase, IDisposable where TItem : class
    {
        [CascadingParameter] public Dashboard<TItem> Dashboard { get; set; }

        [Parameter] public RenderFragment<Dashboard<TItem>> ChildContent { get; set; }
        [Parameter] public EventCallback FilteredDataUpdated { get; set; }


        

        protected override void OnInitialized()
        {
            if(Dashboard == null)
            {
                throw new Exception("Dashboard Component must be in a dashboard");
            }

           // Dashboard.OnUpdate += OnDataFilter;

            base.OnInitialized();
        }

        private void OnDataFilter()
        {
            StateHasChanged();
            FilteredDataUpdated.InvokeAsync();
        }

      

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if(ChildContent != null && Dashboard != null)
            {
                builder.AddContent(1, ChildContent(Dashboard));
            }
           

            base.BuildRenderTree(builder);  
        }

        public void Dispose()
        {
           // DataFactory.OnDataFilter -= OnDataFilter;
        }
    }
}
