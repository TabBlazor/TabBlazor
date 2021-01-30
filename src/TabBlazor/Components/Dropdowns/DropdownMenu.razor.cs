using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public partial class DropdownMenu : TablerBaseComponent
    {
        [Parameter] public int Columns { get; set; } = 1;
        [Parameter] public bool Arrow { get; set; } = false;

        protected override string ClassNames => ClassBuilder
            .Add("dropdown-menu")
.Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .Add($"show")
            .AddIf($"dropdown-menu-arrow", Arrow)
            .AddIf($"dropdown-menu-columns dropdown-menu-columns-{Columns}", Columns > 1)
            .ToString();
    }
}