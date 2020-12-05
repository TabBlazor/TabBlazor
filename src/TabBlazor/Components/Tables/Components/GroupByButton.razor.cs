using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;


namespace TabBlazor.Components.Tables.Components
{
    public partial class GroupByButtonBase<Item> : ComponentBase
    {
        [CascadingParameter(Name = "Table")] public ITable<Item> Table { get; set; }

        //public IList<MenuDropdownItem<IColumn<Item>>> GetDropDownItems()
        //{
        //    var items = new List<MenuDropdownItem<IColumn<Item>>>();
        //    foreach (var column in Table.Columns.Where(e => e.Groupable && !e.GroupBy))
        //    {
        //        items.Add(new MenuDropdownItem<IColumn<Item>> { Title = $"{Loc.GetString(e => e.Table_GroupBy)} {column.Title}", CallBack = CreateCb<IColumn<Item>>(SetGroup), Context = column });
        //    }
        //    return items;
        //}

        protected async Task SetGroup(IColumn<Item> column)
        {
            await column.GroupByMeAsync();
        }
    }
}

