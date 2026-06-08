namespace TabBlazor.Dashboards;

/// <summary>A dashboard facet that filters by distinct values of a property (one filter per value).</summary>
public partial class EqualFacet<TItem> : BaseFacet<TItem> where TItem : class
{
    /// <summary>The property whose distinct values become filter options.</summary>
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

    protected IEnumerable<FacetFilter<TItem>> GetSortedFilters()
    {
        if (SortFilters == null)
        {
            return Facet.Filters.OrderByDescending(e => e.CountAll);
        }

        return SortFilters(Facet.Filters);
    }
}