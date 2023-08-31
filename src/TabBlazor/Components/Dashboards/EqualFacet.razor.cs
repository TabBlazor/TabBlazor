

namespace TabBlazor.Dashboards
{
    public partial class EqualFacet<TItem> : BaseFacet<TItem> where TItem : class
    {
        [Parameter] public Expression<Func<TItem, object>> Expression { get; set; }
      
        protected override void OnInitialized()
        {
            Facet = Dashboard.AddEqualFacet(Expression, Name, FilterLabel);
            base.OnInitialized();
        }

        private void ValueChanged(FacetFilter<TItem> filter, bool value)
        {
            filter.Active = value;
            Dashboard.RunFilter();
        }

    }
}
