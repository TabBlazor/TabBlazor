

namespace TabBlazor.Dashboards
{
    public partial class EqualFacet<TItem> : DashboardComponent<TItem> where TItem : class
    {
        [Parameter] public Expression<Func<TItem, object>> Expression { get; set; }
        [Parameter] public string Name { get; set; }

        [Parameter] public RenderFragment<DataFacet<TItem>> FacetTemplate { get; set; }

        private DataFacet<TItem> facet;

        protected override void OnInitialized()
        {
            facet = Dashboard.AddEqualFacet(Expression, Name);
        }

        private void ResetFilters()
        {
            foreach (var filter in facet.Filters.Where(e => e.Active))
            {
                filter.Active = false;
            }
            Dashboard.RunFilter();
        }

        private void ValueChanged(FacetFilter<TItem> filter, bool value)
        {
            filter.Active = value;
            Dashboard.RunFilter();
        }

    }
}
