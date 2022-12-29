
using System.Reflection;
using TabBlazor.Components.Tables;

namespace TabBlazor.Data
{
    public class DataFactory<TItem> where TItem : class
    {
        private readonly IQueryable<TItem> items;
        private IEnumerable<TItem> filteredItems;
        private readonly List<DataFilter<TItem>> filters = new();
        private readonly List<DataFacet<TItem>> facets = new();


        public IEnumerable<TItem> FilteredItems => filteredItems;
        public IEnumerable<TItem> Items => items;
       
        public DataFactory(IEnumerable<TItem> items)
        {
            this.items = items.AsQueryable();
            FilterData();
        }

        public DataFacet<TItem> AddGroupFacet(Expression<Func<TItem, decimal>> expression, string name, int numberOfGroups)
        {
            var facet = new DataFacet<TItem>();
            facet.Name = name;

            var groups = items.GroupBy(expression).OrderBy(e => e.Key).ToList();
            var count = groups.Count;

            var groupSize = count / numberOfGroups;

            decimal? currentMinValue = null;
            decimal? currentMaxValue = null;

            foreach (var chunkGroup in groups.Chunk(groupSize))
            {

                currentMinValue ??= chunkGroup.Min(e => e.Key);;
                currentMaxValue =chunkGroup.Max(e => e.Key); ;

                var constantMin = Expression.Constant(currentMinValue);
                var bodyMin = Expression.GreaterThanOrEqual(expression.Body, constantMin);
                var predicateMin = Expression.Lambda<Func<TItem, bool>>(bodyMin, expression.Parameters);

             
                var constantMax = Expression.Constant(currentMaxValue);
                var bodyMax = Expression.LessThanOrEqual(expression.Body, constantMax);
                var predicateMax = Expression.Lambda<Func<TItem, bool>>(bodyMax, expression.Parameters);

                var predicate = PredicateBuilder.And(predicateMin, predicateMax);


                var filter = new FacetFilter<TItem>
                {
                    CountAll = chunkGroup.Aggregate(0, (current, group) => current + group.Count()),
                    Filter = new DataFilter<TItem>
                    {
                        Name = $"{currentMinValue} => {currentMaxValue}",
                        Expression = predicate
                    }
                };
                facet.Filters.Add(filter);

                currentMinValue = currentMaxValue + 1;
            }


            facets.Add(facet);
            return facet;

        }


        public DataFacet<TItem> AddEqualFacet(Expression<Func<TItem, object>> expression, string name)
        {
            var facet = new DataFacet<TItem>();
            facet.Name = name;

            var groups = items.GroupBy(expression);

            foreach (var group in groups)
            {
                var constant = Expression.Constant(group.Key);
                var body = Expression.Equal(expression.Body, constant);
                var predicate = Expression.Lambda<Func<TItem, bool>>(body, expression.Parameters);

                var filter = new FacetFilter<TItem>
                {
                    CountAll = group.Count(),
                    Filter = new DataFilter<TItem>
                    {
                        Name = group.Key?.ToString() ?? "null",
                        Expression = predicate
                    }
                };

                facet.Filters.Add(filter);

            }
            facets.Add(facet);
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



            }


            filteredItems = query.ToList();


        }
    }
}




//public IEnumerable<FacetGroup<TItem>> GetFacetsFilters(IEnumerable<IFacet<TItem>> facets)
//{
//    var facetGroups = new List<FacetGroup<TItem>>();

//    foreach (var facet in facets)
//    {
//        var facetGroup = new FacetGroup<TItem>
//        {
//            Facet = facet
//        };

//        facetGroups.Add(facetGroup);

//        var result = items.GroupBy(facet.Property);
//        var filters = new List<FacetFilter<TItem>>();
//        foreach (var group in result)
//        {
//            var filter = new FacetFilter<TItem>
//            {
//                Count = group.Count(),
//                Label = facet.Label == null ? group.Key.ToString() : facet.Label.Compile().Invoke(group.FirstOrDefault())
//            };

//            filters.Add(filter);
//        }
//        facetGroup.Filters = filters;
//    }

//    return facetGroups;
//}


