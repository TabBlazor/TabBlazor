

namespace TabBlazor.Dashboards
{
    public partial class GroupFacet<TItem> : DashboardComponent<TItem> where TItem : class
    {
        [Parameter] public Expression<Func<TItem, decimal>> Expression { get; set; }
        [Parameter] public string Name { get; set; }

        [Parameter] public int NumberOfGroups { get; set; } = 5;

        [Parameter] public RenderFragment<DataFacet<TItem>> Facet { get; set; }

        private DataFacet<TItem> facet;

        protected override void OnInitialized()
        {
            facet = Dashboard.AddGroupFacet(Expression, Name, NumberOfGroups);
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
