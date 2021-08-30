using Microsoft.AspNetCore.Components;
//using Tabler.Components.Table.Components;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TabBlazor.Components.Tables.Components;

namespace TabBlazor.Components.Tables
{
    public interface IColumn<Item>
    {
        ITable<Item> Table { get; set; }
        string Title { get; set; }
        string Width { get; set; }
        string CssClass { get; set; }
        bool Sortable { get; set; }
        bool Searchable { get; set; }
        bool Groupable { get; set; }
        bool Visible { get; set; }
        bool SortDescending { get; }
        bool GroupBy { get; set; }
        Task SortByAsync();
        Task GroupByMeAsync();
        Type Type { get; }
        Expression<Func<Item, object>> Property { get; }
        Expression<Func<Item, string, bool>> SearchExpression { get; }
        object GetValue(Item item);
        Expression<Func<Item, bool>> GetFilter(ITableState<Item> state);
        RenderFragment<Item> EditorTemplate { get; set; }
        RenderFragment<Item> Template { get; set; }
        RenderFragment<TableResult<object, Item>> GroupingTemplate { get; set; }
        bool SortColumn { get; set; }
        bool ActionColumn { get; set; }
    }
}
