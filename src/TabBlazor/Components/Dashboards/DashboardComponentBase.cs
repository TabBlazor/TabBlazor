
using TabBlazor.Data;

namespace TabBlazor.Dashboard
{
    public class DashboardComponent<TItem> :  ComponentBase, IDisposable where TItem : class
    {

        [CascadingParameter] public Dashboard<TItem> Dashboard { get; set; }

        [Parameter] public RenderFragment<DataFactory<TItem>> ChildContent { get; set; }
        [Parameter] public EventCallback FilteredDataUpdated { get; set; }


        public DataFactory<TItem> DataFactory => Dashboard.DataFactory;

        protected override void OnInitialized()
        {
            if(DataFactory == null)
            {
                throw new Exception("Dashboard Component must be in a dashboard");
            }

            DataFactory.OnDataFilter += OnDataFilter;

            base.OnInitialized();
        }

        private void OnDataFilter()
        {
            StateHasChanged();
            FilteredDataUpdated.InvokeAsync();
        }

      

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if(ChildContent != null && DataFactory != null)
            {
                builder.AddContent(1, ChildContent(DataFactory));
            }
           

            base.BuildRenderTree(builder);  
        }

        public void Dispose()
        {
            DataFactory.OnDataFilter -= OnDataFilter;
        }
    }
}
