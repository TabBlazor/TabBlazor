using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public enum TablerTimelineType
    {
        Default,
        Simple
    }

    public partial class TablerTimeline : TablerBaseComponent
    {
        [Parameter] public TablerTimelineType Type { get; set; } = TablerTimelineType.Default;

        protected override string ClassNames => ClassBuilder
            .Add("list list-timeline")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("list-timeline-simple", Type == TablerTimelineType.Simple)
            .ToString();
    }
}