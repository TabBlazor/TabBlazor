

namespace TabBlazor.Dashboards
{
    public partial class GroupFacet<TItem> : BaseFacet<TItem> where TItem : class
    {
        [Parameter] public Expression<Func<TItem, decimal>> Expression { get; set; }
     

        [Parameter] public int NumberOfGroups { get; set; } = 5;

        protected override void OnInitialized()
        {
            Facet = Dashboard.AddGroupFacet(Expression, Name, NumberOfGroups);
            base.OnInitialized();
        }

        private void ValueChanged(FacetFilter<TItem> filter, bool value)
        {
            filter.Active = value;
            Dashboard.RunFilter();
        }

    }
}
