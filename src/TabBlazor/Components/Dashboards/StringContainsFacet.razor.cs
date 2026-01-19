namespace TabBlazor.Dashboards;

public partial class StringContainsFacet<TItem> : BaseFacet<TItem> where TItem : class
{
    [Parameter] public Expression<Func<TItem, IEnumerable<string>>> Expression { get; set; }
    [Parameter] public StringComparer? StringComparer { get; set; }

    protected override void OnInitialized()
    {
        Facet = Dashboard.AddStringContainsFacet(Expression, Name, FilterLabel, StringComparer);
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