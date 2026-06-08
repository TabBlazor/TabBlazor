using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    /// <summary>A single event within a <see cref="Timeline"/>.</summary>
    public partial class TimelineItem : TablerBaseComponent
    {
        /// <summary>Short text shown inside the event icon. Ignored when <see cref="IconTemplate"/> is set.</summary>
        [Parameter] public string IconText { get; set; }
        /// <summary>Optional custom icon content.</summary>
        [Parameter] public RenderFragment IconTemplate { get; set; }
        /// <summary>The color of the event icon.</summary>
        [Parameter] public TablerColor IconColor { get; set; }
        /// <summary>The time/date label for the event.</summary>
        [Parameter] public string Time { get; set; }
        /// <summary>The event title.</summary>
        [Parameter] public string Title { get; set; }

        protected override string ClassNames => ClassBuilder
            .Add("timeline-event")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}