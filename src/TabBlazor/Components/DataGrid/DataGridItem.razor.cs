using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public partial class DataGridItem : TablerBaseComponent
    {
        [Parameter] public string Title { get; set; }
     
        [Parameter] public RenderFragment TitleTemplate { get; set; }


        protected override string ClassNames => ClassBuilder
            .Add("datagrid-item")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}