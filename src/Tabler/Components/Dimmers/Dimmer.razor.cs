using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public partial class Dimmer : TablerBaseComponent
    {
        [Parameter] public bool IsActive { get; set; }
        [Parameter] public bool ShowSpinner { get; set; } = true;

        protected override string ClassNames => ClassBuilder
          .Add("dimmer")
          .AddIf("active", IsActive)
          .ToString();

    }
}
