using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public partial class TimelineItem : TablerBaseComponent
    {
        [Parameter] public string IconText { get; set; }
        [Parameter] public RenderFragment IconTemplate { get; set; }
        [Parameter] public TablerColor IconColor { get; set; }
        [Parameter] public string Time { get; set; }
        [Parameter] public string Title { get; set; }

        protected override string ClassNames => ClassBuilder
            .Add("")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}