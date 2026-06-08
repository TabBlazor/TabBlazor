using Microsoft.AspNetCore.Components;
using TabBlazor.Components.Tables.Components;
using System;
using System.Linq;
using System.Threading.Tasks;
using TabBlazor.Components.Tables;
using TabBlazor;
using LinqKit;

namespace TabBlazor
{
    /// <summary>
    /// A column definition for the standard <c>Table</c> component. Bind <see cref="Property"/> for sorting,
    /// searching and grouping, or supply a <see cref="Template"/> for custom cell content.
    /// <typeparamref name="Item"/> is the table row type.
    /// </summary>
    public class ColumnBase<Item> : ComponentBase, IColumn<Item>
    {
        [Inject] protected TableFilterService FilterService { get; set; }

        private string _title;

        /// <summary>The column header text. Falls back to the bound <see cref="Property"/> name when not set.</summary>
        [Parameter]
        public string Title
        {
            get { return _title ?? Property.GetPropertyMemberInfo()?.Name; }
            set { _title = value; }
        }

        /// <summary>The owning table, supplied via cascading parameter.</summary>
        [CascadingParameter(Name = "Table")] public ITable<Item> Table { get; set; }
        /// <summary>Optional fixed column width (CSS value).</summary>
        [Parameter] public string Width { get; set; }
        /// <summary>When true, the column can be sorted. Requires <see cref="Property"/>. Defaults to false.</summary>
        [Parameter] public bool Sortable { get; set; }
        /// <summary>When true, the column participates in search. Requires <see cref="Property"/>. Defaults to false.</summary>
        [Parameter] public bool Searchable { get; set; }
        /// <summary>When true, the table can be grouped by this column. Defaults to false.</summary>
        [Parameter] public bool Groupable { get; set; }
        /// <summary>Additional CSS class(es) for the column's cells.</summary>
        [Parameter] public string CssClass { get; set; }
        /// <summary>Whether the column is visible. Defaults to true.</summary>
        [Parameter] public bool Visible { get; set; } = true;
        /// <summary>When true, marks this as the row-actions column. Defaults to false.</summary>
        [Parameter] public bool ActionColumn { get; set; }
        /// <summary>Optional custom header content, overriding <see cref="Title"/>.</summary>
        [Parameter] public RenderFragment HeaderTemplate { get; set; }
        /// <summary>Custom cell content for the row. When omitted, the bound property value is shown.</summary>
        [Parameter] public RenderFragment<Item> Template { get; set; }
        /// <summary>Custom editor content shown while editing the row.</summary>
        [Parameter] public RenderFragment<Item> EditorTemplate { get; set; }
        /// <summary>Custom content for the group header row when grouping by this column.</summary>
        [Parameter] public RenderFragment<TableResult<object, Item>> GroupingTemplate { get; set; }
        /// <summary>The property this column binds to, e.g. <c>@(x => x.Name)</c>. Required for sort/search/group.</summary>
        [Parameter] public Expression<Func<Item, object>> Property { get; set; }
        /// <summary>Optional custom search predicate, overriding the default contains search.</summary>
        [Parameter] public Expression<Func<Item, string, bool>> SearchExpression { get; set; }
        /// <summary>Initial sort order for this column, if any.</summary>
        [Parameter] public SortOrder? Sort { get; set; }
        /// <summary>Cell content alignment.</summary>
        [Parameter] public Align Align { get; set; }
        /// <summary>When true, the table starts grouped by this column. Defaults to false.</summary>
        [Parameter] public bool Group { get; set; }

        public bool SortColumn { get; set; }
        public bool GroupBy { get; set; }
        public bool SortDescending { get; set; }
        public Type Type { get; private set; }


        public void Dispose()
        {
            Table.RemoveColumn(this);
        }

        protected override void OnInitialized()
        {
            GroupBy = Group;

            if (Sort != null)
            {
                SortColumn = true;
                SortDescending = Sort == SortOrder.Descending;
            }

            Table.AddColumn(this);
        }

        protected override void OnParametersSet()
        {
            if ((Sortable && Property == null) || (Searchable && Property == null))
            {
                throw new InvalidOperationException($"Column {Title} Property parameter is null");
            }

            if (Title == null && Property == null)
            {
                throw new InvalidOperationException("A Column has both Title and Property parameters null");
            }

            Type = Property?.GetPropertyMemberInfo().GetMemberUnderlyingType();

            PropertyNullSafe = (Expression<Func<Item, object>>)Property?.PropagateNull();

        }

        public Expression<Func<Item, object>> PropertyNullSafe { get; private set; }

        //private Expression<Func<Item, object>> propertyNullSafe;
        //public Expression<Func<Item, object>> PropertyNullSafe
        //{
        //    get
        //    {
        //        if (Property == null) { return null; }
        //        propertyNullSafe ??= (Expression<Func<Item, object>>)Property.PropagateNull();
        //        return propertyNullSafe;
        //    }
        //}

        public Expression<Func<Item, bool>> GetFilter(ITableState<Item> state)
        {
            if ((Searchable || SearchExpression != null) && Property != null && !string.IsNullOrEmpty(state.SearchText))
            {
                var filter = FilterService.GetFilter(this, state.SearchText);
                if (filter == null)
                {
                    return null;
                }

                return PredicateBuilder
                    .New<Item>()
                    .And(NotNull())
                    .And(filter);
            }

            return null;
        }

        private Expression<Func<Item, bool>> NotNull()
        {
            return Expression.Lambda<Func<Item, bool>>(
                Expression.NotEqual(PropertyNullSafe.Body, Expression.Constant(null)),
                PropertyNullSafe.Parameters.ToArray()
            );
        }

        public async Task GroupByMeAsync()
        {
            if (Groupable)
            {
                if (GroupBy)
                {
                    GroupBy = false;
                    Visible = true;
                }
                else
                {
                    foreach (var column in Table.Columns.Where(e => e.GroupBy))
                    {
                        column.GroupBy = false;
                        column.Visible = true;
                    }
                    GroupBy = true;
                    Visible = false;
                }

                await Table.Update(true);
            }
        }

        public async Task SortByAsync()
        {
            if (Sortable)
            {
                var sortOnColumn = true;
                if (SortColumn)
                {
                    if (SortDescending && Table.ResetSortCycle)
                    {
                        sortOnColumn = false;
                    }
                    SortDescending = !SortDescending;
                }

                Table.Columns.ForEach(x => x.SortColumn = false);

                SortColumn = sortOnColumn;
                await Table.Update();
            }
        }

        public object GetValue(Item item)
        {
            try
            {
              return PropertyNullSafe.Compile().Invoke(item);
               // return Property.Compile().Invoke(item);
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }
    }
}