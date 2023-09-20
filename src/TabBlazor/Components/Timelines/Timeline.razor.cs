using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public enum TimelineType
    {
        Default,
        Simple
    }

    public partial class Timeline : TablerBaseComponent
    {
        [Parameter] public TimelineType Type { get; set; } = TimelineType.Default;

        protected override string ClassNames => ClassBuilder
            .Add("timeline")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("timeline-simple", Type == TimelineType.Simple)
            .ToString();
    }
}