using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public partial class DropdownItem : TablerBaseComponent
    {
        [Parameter] public bool IsActive { get; set; }

        protected override string ClassNames => ClassBuilder
            .Add("dropdown-item")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("active", IsActive)
            .ToString();
    }
}