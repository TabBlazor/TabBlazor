

namespace TabBlazor.Dashboards
{
    /// <summary>A dashboard facet that buckets a numeric property into a fixed number of range groups.</summary>
    public partial class GroupFacet<TItem> : BaseFacet<TItem> where TItem : class
    {
        /// <summary>The numeric property to group on.</summary>
        [Parameter] public Expression<Func<TItem, decimal>> Expression { get; set; }


        /// <summary>The number of range buckets to create. Defaults to 5.</summary>
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
