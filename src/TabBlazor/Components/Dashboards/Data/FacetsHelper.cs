
using System;
using System.Linq;
using TabBlazor.Dashboards.Extensions;

namespace TabBlazor.Dashboards
{
    internal static class FacetsHelper
    {

        public static Expression<Func<TItem, bool>> CreateDateRangePredicate<TItem>(Expression<Func<TItem, DateTime>> expression, DateTime min, DateTime max) where TItem : class
        {
            var constantMin = Expression.Constant(min);
            var bodyMin = Expression.GreaterThanOrEqual(expression.Body, constantMin);
            var predicateMin = Expression.Lambda<Func<TItem, bool>>(bodyMin, expression.Parameters);

            var constantMax = Expression.Constant(max);
            var bodyMax = Expression.LessThanOrEqual(expression.Body, constantMax);
            var predicateMax = Expression.Lambda<Func<TItem, bool>>(bodyMax, expression.Parameters);

            return PredicateBuilder.And(predicateMin, predicateMax);
        }

        public static Expression<Func<TItem, bool>> CreateRangePredicate<TItem>(Expression<Func<TItem, decimal>> expression, decimal min, decimal max) where TItem : class
        {
            var constantMin = Expression.Constant(min);
            var bodyMin = Expression.GreaterThanOrEqual(expression.Body, constantMin);
            var predicateMin = Expression.Lambda<Func<TItem, bool>>(bodyMin, expression.Parameters);

            var constantMax = Expression.Constant(max);
            var bodyMax = Expression.LessThanOrEqual(expression.Body, constantMax);
            var predicateMax = Expression.Lambda<Func<TItem, bool>>(bodyMax, expression.Parameters);

            return PredicateBuilder.And(predicateMin, predicateMax);

        }




        public static DataFacet<TItem> AddDateFacet<TItem>(IQueryable<TItem> items, Expression<Func<TItem, DateTime>> expression, string name) where TItem : class
        {
            var facet = new DataFacet<TItem>
            {
                Name = name
            };

            var min = items.Min(expression);
            var max = items.Max(expression);

            var dates = DateRangeGenerator.Generate();

            foreach (var dateRange in dates)
            {
                var predicate = CreateDateRangePredicate(expression, dateRange.Start, dateRange.End);

                var groupItems = items.Where(predicate).ToList();

                var filter = new FacetFilter<TItem>
                {
                    Items = groupItems,
                    CountAll = groupItems.Count,
                    Filter = new DataFilter<TItem>
                    {
                        Name = dateRange.Name,
                        Expression = predicate
                    }
                };
                facet.Filters.Add(filter);
            }

            return facet;
        }


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

                var predicate = CreateRangePredicate(expression, groupMin, groupMax);

                //var constantMin = Expression.Constant(groupMin);
                //var bodyMin = Expression.GreaterThanOrEqual(expression.Body, constantMin);
                //var predicateMin = Expression.Lambda<Func<TItem, bool>>(bodyMin, expression.Parameters);


                //var constantMax = Expression.Constant(groupMax);
                //var bodyMax = Expression.LessThanOrEqual(expression.Body, constantMax);
                //var predicateMax = Expression.Lambda<Func<TItem, bool>>(bodyMax, expression.Parameters);

                //var predicate = PredicateBuilder.And(predicateMin, predicateMax);

                var groupItems = chunkGroup.ToList().SelectMany(e => e.ToList()).ToList();

                var filter = new FacetFilter<TItem>
                {
                    Items = groupItems,
                    CountAll = groupItems.Count,  
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

        private static LambdaExpression StripConvert<T>(Expression<Func<T, object>> source)
        {
            Expression result = source.Body;
            // use a loop in case there are nested Convert expressions for some crazy reason
            while (((result.NodeType == ExpressionType.Convert)
                       || (result.NodeType == ExpressionType.ConvertChecked))
                   && (result.Type == typeof(object)))
            {
                result = ((UnaryExpression)result).Operand;
            }
            return Expression.Lambda(result, source.Parameters);
        }

        public static DataFacet<TItem> AddEqualFacet<TItem>(IQueryable<TItem> items, Expression<Func<TItem, object>> expression, string name, Func<FacetFilter<TItem>, string> filterLabel) where TItem : class
        {
            var facet = new DataFacet<TItem>();
            facet.Name = name;

            var groups = items.GroupBy(expression);

            foreach (var group in groups)
            {
                var unConvertExp = StripConvert<TItem>(expression);

                var constant = Expression.Constant(group.Key);
                var body = Expression.Equal(unConvertExp.Body, constant);
                var predicate = Expression.Lambda<Func<TItem, bool>>(body, unConvertExp.Parameters);

                var filter = new FacetFilter<TItem>
                {
                    FilterLabel = filterLabel,
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
