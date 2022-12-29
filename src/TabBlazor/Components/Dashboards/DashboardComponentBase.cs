
using TabBlazor.Data;

namespace TabBlazor.Dashboard
{
    public abstract class DashboardComponentBase<TItem> : ComponentBase where TItem : class
    {

        [CascadingParameter] public DataFactory<TItem> DataFactory { get; set; }

        
        protected override void OnInitialized()
        {
            if(DataFactory == null)
            {
                throw new Exception("Dashboard Component must be in a dashboard");
            }

            base.OnInitialized();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {


            base.BuildRenderTree(builder);  
        }

    }
}
