using System.Diagnostics;

namespace TabBlazor.Dashboards;

public partial class Dashboard<TItem> where TItem : class
{
    private readonly List<DataFacet<TItem>> facets = new();
    private readonly List<DataFilter<TItem>> filters = new();

    public long LastRunFilterMilliseconds;
    [Parameter] public IEnumerable<TItem> Items { get; set; }
    [Parameter] public RenderFragment<Dashboard<TItem>> ChildContent { get; set; }

    [Parameter] public EventCallback OnUpdate { get; set; }

    [Parameter] public bool Debug { get; set; }

    public IQueryable<TItem> FilteredItems { get; private set; }

    public IQueryable<TItem> AllItems { get; private set; }


    protected override void OnInitialized()
    {
        AllItems = Items.AsQueryable();
    }

    public void RemoveFacet(DataFacet<TItem> facet)
    {
        if (facets.Contains(facet))
        {
            facets.Remove(facet);
            RunFilter();
        }
    }

    public DataFacet<TItem> AddEqualFacet(Expression<Func<TItem, object>> expression, string name,
        Func<FacetFilter<TItem>, string> filterLabel)
    {
        var facet = FacetsHelper.AddEqualFacet(AllItems, expression, name, filterLabel);
        facets.Add(facet);
        RunFilter();
        return facet;
    }

    public DataFacet<TItem> AddStringContainsFacet(Expression<Func<TItem, IEnumerable<string>>> expression, string name,
        Func<FacetFilter<TItem>, string> filterLabel, StringComparer stringComparer = null)
    {
        var facet = FacetsHelper.AddStringContainsFacet(AllItems, expression, name, filterLabel);
        facets.Add(facet);
        RunFilter();
        return facet;
    }

    public DataFacet<TItem> AddDateFacet(Expression<Func<TItem, DateTime>> expression, string name)
    {
        var facet = FacetsHelper.AddDateFacet(AllItems, expression, name);
        facets.Add(facet);

        return facet;
    }

    public DataFacet<TItem> AddGroupFacet(Expression<Func<TItem, decimal>> expression, string name,
        int numberOfGroups)
    {
        var facet = FacetsHelper.AddGroupFacet(AllItems, expression, name, numberOfGroups);
        facets.Add(facet);
        RunFilter();
        return facet;
    }

    public DataFacet<TItem> AddFacet(DataFacet<TItem> facet)
    {
        facets.Add(facet);
        RunFilter();
        return facet;
    }

    public void AddFilter(DataFilter<TItem> dataFilter)
    {
        filters.Add(dataFilter);
        RunFilter();
    }

    public void RemoveFilter(DataFilter<TItem> dataFilter)
    {
        if (filters.Contains(dataFilter))
        {
            filters.Remove(dataFilter);
            RunFilter();
        }
    }

    private void WriteDebug(string text, Stopwatch sw)
    {
        if (Debug)
        {
            Console.WriteLine($"{text}: {sw.ElapsedMilliseconds}");
        }
    }


    public void RunFilter()
    {
        var sw = new Stopwatch();
        sw.Start();

        var query = AllItems.AsQueryable();
        WriteDebug("1", sw);


        foreach (var filter in filters)
        {
            query = query.Where(filter.Expression);
        }

        foreach (var facet in facets)
        {
            Expression<Func<TItem, bool>> predicate = null;

            foreach (var filter in facet.Filters.Where(e => e.Active))
            {
                if (predicate == null)
                {
                    predicate = PredicateBuilder.Create(filter.Filter.Expression);
                }
                else
                {
                    predicate = predicate.Or(filter.Filter.Expression);
                }
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }
        }

        WriteDebug("2", sw);
        FilteredItems = query.ToList().AsQueryable();
        WriteDebug("3", sw);

        SetFacetFilterCount();
        WriteDebug("4", sw);

        OnUpdate.InvokeAsync();
        StateHasChanged();

        sw.Stop();
        LastRunFilterMilliseconds = sw.ElapsedMilliseconds;
    }


    private void SetFacetFilterCount()
    {
        foreach (var facet in facets)
        {
            foreach (var filter in facet.Filters)
            {
                filter.FilteredItems = FilteredItems.Where(filter.Filter.Predicate);
            }
        }
    }
}