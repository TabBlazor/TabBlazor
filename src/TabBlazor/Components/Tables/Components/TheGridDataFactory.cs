using System.Text.RegularExpressions;
using LinqKit;
using TabBlazor.Components.Tables.Components;

namespace TabBlazor.Components.Tables
{
    public partial class TheGridDataFactory<Item> : IDataProvider<Item>
    {
        public async Task<IEnumerable<TableResult<object, Item>>> GetData(List<IColumn<Item>> columns, ITableState<Item> state, IEnumerable<Item> items, bool resetPage = false, bool addSorting = true,
            Item moveToItem = default)
        {
            var viewResult = new List<TableResult<object, Item>>();
            if (items != null)
            {
                var query = items.AsQueryable();
                query = AddSearch(columns, state, query);
                if (addSorting)
                {
                    query = AddSorting(columns, state, query);
                }

                state.TotalCount = query.Count();
                if (resetPage)
                {
                    state.PageNumber = 0;
                }
                else if (state.TotalCount == 0)
                {
                    state.PageNumber = 0;
                }
                else if (state.TotalCount - 1 < state.PageSize * state.PageNumber)
                {
                    state.PageNumber = (int) Math.Floor((decimal) (state.TotalCount / state.PageSize));
                }
                else if (moveToItem != null)
                {
                    var pos = query.ToList().IndexOf(moveToItem);
                    if (pos > 0)
                    {
                        state.PageNumber = (int) Math.Floor((decimal) (pos / state.PageSize));
                    }
                }

                query = query.Skip(state.PageNumber * state.PageSize).Take(state.PageSize);
                var columnGroup = columns.FirstOrDefault(e => e.GroupBy);
                if (columnGroup == null)
                {
                    viewResult.Add(new TableResult<object, Item>(null, query.ToList()));
                }
                else
                {
                    columnGroup.GroupBy = true;
                    foreach (var r in query.GroupBy(columnGroup.Property))
                    {
                        viewResult.Add(new TableResult<object, Item>(r.Key, r.ToList())
                        {
                            GroupingTemplate = columnGroup.GroupingTemplate
                        });
                    }
                }
            }

            return await Task.FromResult(viewResult);
        }

        private IQueryable<Item> AddSorting(List<IColumn<Item>> columns, ITableState<Item> state, IQueryable<Item> query)
        {
            var sortColumn = columns.FirstOrDefault(x => x.SortColumn);
            if (sortColumn != null)
            {
                if (state.UseNaturalSort)
                {
                    query = NaturalOrderBy(query, sortColumn.Property, sortColumn.SortDescending);
                }
                else
                {
                    query = sortColumn.SortDescending
                        ? query.OrderByDescending(sortColumn.Property)
                        : query.OrderBy(sortColumn.Property);
                }
            }

            return query;
        }

        [GeneratedRegex("\\d+")]
        private static partial Regex DigitRegex();

        private static IQueryable<T> NaturalOrderBy<T>(IQueryable<T> source, Expression<Func<T, object>> selectorExpr, bool desc)
        {
            var selector = selectorExpr.Compile();
            var max = source
                .SelectMany(i => DigitRegex().Matches(selector(i).ToString()).Select(m => (int?) m.Value.Length))
                .Max() ?? 0;
            Expression<Func<T, string>> keySelector = i => DigitRegex().Replace(selector(i).ToString(), m => m.Value.PadLeft(max, '0'));
            return desc ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
        }

        private static IQueryable<Item> AddSearch(List<IColumn<Item>> columns, ITableState<Item> state, IQueryable<Item> query)
        {
            if (string.IsNullOrEmpty(state.SearchText))
            {
                return query;
            }

            var predicate = PredicateBuilder.New<Item>();
            foreach (var column in columns.Where(c => c.Searchable))
            {
                var filter = column.GetFilter(state);
                if (filter != null)
                {
                    predicate = predicate.Or(filter);
                }
            }

            if (state.CurrentEditItem != null)
            {
                predicate = predicate.Or(e => e.Equals(state.CurrentEditItem));
            }

            query = query.Where(predicate);
            return query;
        }
    }
}