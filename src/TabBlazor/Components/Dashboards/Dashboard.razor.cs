
namespace TabBlazor.Dashboards
{
    public partial class Dashboard<TItem> where TItem : class
    {
        [Parameter] public IEnumerable<TItem> Items { get; set; }
        [Parameter] public RenderFragment<Dashboard<TItem>> ChildContent { get; set; }

        [Parameter] public EventCallback OnUpdate { get; set; }

        public IEnumerable<TItem> FilteredItems => filteredItems;

        private IQueryable<TItem> items;
        private IEnumerable<TItem> filteredItems;
        private readonly List<DataFilter<TItem>> filters = new();
        private readonly List<DataFacet<TItem>> facets = new();


        protected override void OnInitialized()
        {
            items = Items.AsQueryable();
        }

    
        public void RemoveFacet(DataFacet<TItem> facet)
        {
            if (facets.Contains(facet))
            {
                facets.Remove(facet);
                FilterData();
            }
            
        }

        public DataFacet<TItem> AddEqualFacet(Expression<Func<TItem, object>> expression, string name)
        {
            var facet = FacetsHelper.AddEqualFacet(items, expression, name);
            facets.Add(facet);
            FilterData();
            return facet;
        }

        public DataFacet<TItem> AddGroupFacet(Expression<Func<TItem, decimal>> expression, string name, int numberOfGroups)
        {
            var facet = FacetsHelper.AddGroupFacet(items, expression, name, numberOfGroups);
            facets.Add(facet);
            FilterData();
            return facet;
        }



        public void AddFilter(DataFilter<TItem> dataFilter)
        {
            filters.Add(dataFilter);
            FilterData();
        }

        public void RemoveFilter(DataFilter<TItem> dataFilter)
        {
            if (filters.Contains(dataFilter))
            {
                filters.Remove(dataFilter);
                FilterData();
            }
        }

        public void FilterData()
        {
            var query = items.AsQueryable();

            foreach (var filter in filters)
            {
                var test = filter.Expression.ToString();
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

                filteredItems = query.ToList();
            }

            OnUpdate.InvokeAsync();
            StateHasChanged();
        }
    }
}