using Microsoft.AspNetCore.Components.Web;

namespace TabBlazor.Dashboards
{
    /// <summary>A dashboard facet that filters by date buckets derived from a <see cref="DateTime"/> property.</summary>
    public partial class DateFacet<TItem> : BaseFacet<TItem> where TItem : class
    {
        /// <summary>The date property to facet on.</summary>
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
