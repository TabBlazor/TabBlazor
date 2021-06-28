using Microsoft.AspNetCore.Components;
using TabBlazor.Components.Tables;

namespace TabBlazor.Components.Tables
{
    public class TableHeaderToolsBase<TableItem> : ComponentBase
    {
        [CascadingParameter(Name = "Table")] public ITable<TableItem> Table { get; set; }

    }
}