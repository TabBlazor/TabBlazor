using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public partial class DataGrid : TablerBaseComponent
    {
     
        protected override string ClassNames => ClassBuilder
            .Add("datagrid")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}