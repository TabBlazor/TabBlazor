using Microsoft.AspNetCore.Components;

namespace TabBlazor
{

    /// <summary>A status indicator (pulsing ring). Color comes from <see cref="TablerBaseComponent.BackgroundColor"/>.</summary>
    public partial class StatusIndicator : TablerBaseComponent
    {
        /// <summary>When true, the indicator animates. Defaults to false.</summary>
        [Parameter] public bool Animate { get; set; }


        protected override string ClassNames => ClassBuilder
            .Add("status-indicator")
            .Add(BackgroundColor.GetColorClass("status", ColorType.Default))
            .AddIf("status-indicator-animated", Animate)
            .AddIf("cursor-pointer", OnClick.HasDelegate)
            .ToString();
    }
}
