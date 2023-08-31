using Microsoft.AspNetCore.Components;

namespace TabBlazor
{

    public partial class StatusIndicator : TablerBaseComponent
    {
        [Parameter] public bool Animate { get; set; }


        protected override string ClassNames => ClassBuilder
            .Add("status-indicator")
            .Add(BackgroundColor.GetColorClass("status", ColorType.Default))
            .AddIf("status-indicator-animated", Animate)
            .AddIf("cursor-pointer", OnClick.HasDelegate)
            .ToString();
    }
}
