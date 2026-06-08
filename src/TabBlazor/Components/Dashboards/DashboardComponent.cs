

namespace TabBlazor.Dashboards
{
    /// <summary>
    /// Base class for components that live inside a <see cref="Dashboard{TItem}"/> and access it via cascading
    /// parameter. Throws if used outside a dashboard.
    /// </summary>
    public class DashboardComponent<TItem> :  ComponentBase where TItem : class
    {
        /// <summary>The owning dashboard, supplied via cascading parameter.</summary>
        [CascadingParameter] public Dashboard<TItem> Dashboard { get; set; }
        /// <summary>Content rendered with the dashboard as context.</summary>
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
