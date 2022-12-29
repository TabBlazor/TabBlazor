using TabBlazor.Data;

namespace TabBlazor.Dashboard
{
    public partial class DashboardFacet<TItem> : DashboardComponent<TItem> where TItem : class
    {
        [Parameter] public Expression<Func<TItem, object>> Expression { get; set; }
        [Parameter] public string Name { get; set; }

        [Parameter] public RenderFragment<DataFacet<TItem>> Facet { get; set; }

        private DataFacet<TItem> facet;

        protected override void OnInitialized()
        {
            facet = DataFactory.AddEqualFacet(Expression, Name);
        }

        private void ValueChanged(FacetFilter<TItem> filter, bool value)
        {
            filter.Active = value;
            DataFactory.FilterData();
        }

    }
}
