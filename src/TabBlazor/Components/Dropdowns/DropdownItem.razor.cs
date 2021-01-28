using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public partial class DropdownItem : TablerBaseComponent
    {
        [Parameter] public bool Active { get; set; }
        [Parameter] public bool Disabled { get; set; }

        protected override string ClassNames => ClassBuilder
            .Add("dropdown-item")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("active", Active)
             .AddIf("disabled", Disabled)
            .ToString();
    }
}