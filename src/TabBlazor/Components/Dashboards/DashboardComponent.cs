

namespace TabBlazor.Dashboards
{
    public class DashboardComponent<TItem> :  ComponentBase where TItem : class
    {
        [CascadingParameter] public Dashboard<TItem> Dashboard { get; set; }
        [Parameter] public RenderFragment<Dashboard<TItem>> ChildContent { get; set; }
      

        protected override void OnInitialized()
        {
            if(Dashboard == null)
            {
                throw new Exception("Dashboard Component must be in a dashboard");
            }

            base.OnInitialized();
        }
     

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if(ChildContent != null && Dashboard != null)
            {
                builder.AddContent(1, ChildContent(Dashboard));
            }
           

            base.BuildRenderTree(builder);  
        }

  
    }
}
