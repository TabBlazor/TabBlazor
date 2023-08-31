using Microsoft.AspNetCore.Components.Web;

namespace TabBlazor.Dashboards
{
    public partial class DateFacet<TItem> : BaseFacet<TItem> where TItem : class
    {
        [Parameter] public Expression<Func<TItem, DateTime>> Expression { get; set; }
        

        protected override void OnInitialized()
        {
            Facet = Dashboard.AddDateFacet(Expression, Name);
            base.OnInitialized();
        }

        private void ValueChanged(FacetFilter<TItem> filter, MouseEventArgs e)
        {
            if(!filter.Active)
            {
                ResetFilters(false);
                filter.Active= true;
            }
            else
            {
                filter.Active= false;
            }
         
            Dashboard.RunFilter();
        }

    }
}
