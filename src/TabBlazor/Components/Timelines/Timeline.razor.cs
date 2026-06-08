using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    /// <summary>The visual style of a <see cref="Timeline"/>.</summary>
    public enum TimelineType
    {
        /// <summary>Standard timeline.</summary>
        Default,
        /// <summary>Compact, simplified timeline.</summary>
        Simple
    }

    /// <summary>A vertical list of <see cref="TimelineItem"/> events.</summary>
    public partial class Timeline : TablerBaseComponent
    {
        /// <summary>The timeline style. Defaults to <see cref="TimelineType.Default"/>.</summary>
        [Parameter] public TimelineType Type { get; set; } = TimelineType.Default;

        protected override string ClassNames => ClassBuilder
            .Add("timeline")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("timeline-simple", Type == TimelineType.Simple)
            .ToString();
    }
}