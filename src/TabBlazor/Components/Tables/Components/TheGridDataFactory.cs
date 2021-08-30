using LinqKit;
using System;
using System.Collections.Generic;
using System.Linq;
using TabBlazor.Components.Tables.Components;

namespace TabBlazor.Components.Tables
{
    public class TheGridDataFactory<Item>
    {
        private readonly List<IColumn<Item>> columns;
        private readonly ITableState<Item> state;

        public TheGridDataFactory(List<IColumn<Item>> columns, ITableState<Item> state)
        {
            this.columns = columns;
            this.state = state;
        }

        public IEnumerable<TableResult<object, Item>> GetData(IEnumerable<Item> items, bool resetPage = false, bool addSorting = true, Item moveToItem = default)
        {
            var viewResult = new List<TableResult<object, Item>>();
            if (items != null)
            {
                var query = items.AsQueryable();
                query = AddSearch(query);
                //if (state.CurrentEditItem == null)
                //{
                //    query = AddSearch(query);
                //}


                if (addSorting)
                {
                    query = AddSorting(query);
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

                else if ((state.TotalCount - 1) < state.PageSize * state.PageNumber)
                {
                    state.PageNumber = (int)Math.Floor((decimal)(state.TotalCount / state.PageSize)) - 1;
                }
                else if (moveToItem != null)
                {
                    var pos = query.ToList().IndexOf(moveToItem);
                    if (pos > 0)
                    {
                        state.PageNumber = (int)Math.Floor((decimal)(pos / state.PageSize));
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
