using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    /// <summary>
    /// Overlays its content with a dimming layer and optional spinner, used to indicate a loading/busy state.
    /// </summary>
    public partial class Dimmer : TablerBaseComponent
    {
        /// <summary>When true, the dimmer overlay is shown. Defaults to false.</summary>
        [Parameter] public bool Active { get; set; }
        /// <summary>When true, shows a spinner while active. Defaults to true.</summary>
        [Parameter] public bool ShowSpinner { get; set; } = true;

        protected override string ClassNames => ClassBuilder
          .Add("dimmer")
          .AddIf("active", Active)
          .ToString();

    }
}
