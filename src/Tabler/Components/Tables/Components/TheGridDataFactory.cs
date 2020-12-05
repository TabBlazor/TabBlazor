using LinqKit;
using System.Collections.Generic;
using System.Linq;
using TabBlazor.Components.Tables.Components;

namespace TabBlazor.Components.Tables
{
    public class TheGridDataFactory<Item>
    {
        private readonly List<IColumn<Item>> columns;
        private readonly ITableState state;

        public TheGridDataFactory(List<IColumn<Item>> columns, ITableState state)
        {
            this.columns = columns;
            this.state = state;
        }

        public IEnumerable<TableResult<object, Item>> GetData(IEnumerable<Item> items, bool resetPage = false)
        {
            var viewResult = new List<TableResult<object, Item>>();
            if (items != null)
            {
                var query = items.AsQueryable();
                query = AddSearch(query);
                query = AddSorting(query);
                state.TotalCount = query.Count();

                if (resetPage)
                {
                    state.PageNumber = 0;
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

            return viewResult;
        }

        private IQueryable<Item> AddSorting(IQueryable<Item> query)
        {
            var sortColumn = columns.FirstOrDefault(x => x.SortColumn);
            if (sortColumn != null)
            {
                query = sortColumn.SortDescending
                    ? query.OrderByDescending(sortColumn.Property)
                    : query.OrderBy(sortColumn.Property);
            }

            return query;
        }

        private IQueryable<Item> AddSearch(IQueryable<Item> query)
        {
            var predicate = PredicateBuilder.New<Item>();
            foreach (var column in columns.Where(c => c.Searchable))
            {
                var filter = column.GetFilter(state);
                if (filter != null)
                {
                    predicate = predicate.Or(filter);
                }
            }

            if (!string.IsNullOrEmpty(state.SearchText))
            {
                query = query.Where(predicate);
            }

            return query;
        }
    }
}
