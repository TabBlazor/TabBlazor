using Microsoft.AspNetCore.Components;
using Tabler.Components.Tables;

namespace Tabler.Components.Tables
{
    public class TableHeaderToolsBase<TableItem> : ComponentBase
    {
        [CascadingParameter(Name = "Table")] public ITable<TableItem> Table { get; set; }
    }
}