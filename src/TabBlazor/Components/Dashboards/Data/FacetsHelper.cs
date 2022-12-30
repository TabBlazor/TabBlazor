
using System;

namespace TabBlazor.Dashboards
{
    internal static class FacetsHelper
    {

        public static DataFacet<TItem> AddGroupFacet<TItem>(IQueryable<TItem> items, Expression<Func<TItem, decimal>> expression, string name, int numberOfGroups) where TItem : class
        {

            var numberOfDecimals = 0;

            var facet = new DataFacet<TItem>();
            facet.Name = name;

            var groups = items.GroupBy(expression).OrderBy(e => e.Key).ToList();
            var groupSize = (groups.Count / numberOfGroups);
           
            foreach (var chunkGroup in groups.Chunk(groupSize))
            {
                var groupMax = chunkGroup.Max(e => e.Key);
                var groupMin = chunkGroup.Min(e => e.Key);

                var constantMin = Expression.Constant(groupMin);
                var bodyMin = Expression.GreaterThanOrEqual(expression.Body, constantMin);
                var predicateMin = Expression.Lambda<Func<TItem, bool>>(bodyMin, expression.Parameters);


                var constantMax = Expression.Constant(groupMax);
                var bodyMax = Expression.LessThanOrEqual(expression.Body, constantMax);
                var predicateMax = Expression.Lambda<Func<TItem, bool>>(bodyMax, expression.Parameters);

                var predicate = PredicateBuilder.And(predicateMin, predicateMax);

                var groupItems = chunkGroup.ToList().SelectMany(e => e.ToList()).ToList(); 

                var filter = new FacetFilter<TItem>
                {
                    Items = groupItems,
                    CountAll = groupItems.Count,  //chunkGroup.Aggregate(0, (current, group) => current + group.Count()),
                    Filter = new DataFilter<TItem>
                    {
                        Name = $"{groupMin.ToString($"n{numberOfDecimals}")} => {groupMax.ToString($"n{numberOfDecimals}")}",
                        Expression = predicate
                    }
                };
                facet.Filters.Add(filter);

            }

            return facet;

        }


        public static DataFacet<TItem> AddEqualFacet<TItem>(IQueryable<TItem> items, Expression<Func<TItem, object>> expression, string name) where TItem : class
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
                    Items = group.ToList(),
                    CountAll = group.Count(),
                    Filter = new DataFilter<TItem>
                    {
                        Name = group.Key?.ToString() ?? "null",
                        Expression = predicate
                    }
                };

                facet.Filters.Add(filter);

            }

            return facet;

        }


    }
}
