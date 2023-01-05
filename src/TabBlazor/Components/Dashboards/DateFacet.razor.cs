

using Microsoft.AspNetCore.Components.Web;

namespace TabBlazor.Dashboards
{
    public partial class DateFacet<TItem> : DashboardComponent<TItem> where TItem : class
    {
        [Parameter] public Expression<Func<TItem, DateTime>> Expression { get; set; }
        [Parameter] public string Name { get; set; }

        [Parameter] public RenderFragment<DataFacet<TItem>> FacetTemplate { get; set; }

        private DataFacet<TItem> facet;

        protected override void OnInitialized()
        {
            facet = Dashboard.AddDateFacet(Expression, Name);
        }

        private void ResetFilters(bool runFilter)
        {
            foreach (var filter in facet.Filters.Where(e => e.Active))
            {
                filter.Active = false;
            }
            if(runFilter)
            {
                Dashboard.RunFilter();
            }
           
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
