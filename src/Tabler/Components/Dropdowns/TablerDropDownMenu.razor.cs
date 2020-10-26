using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public partial class TablerDropDownMenu : TablerBaseComponent
    {
        [Parameter] public int Columns { get; set; } = 1;
        [Parameter] public bool Arrow { get; set; } = false;
        [Parameter] public bool Show { get; set; } = false;

        protected override string ClassNames => ClassBuilder
            .Add("dropdown-menu")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf($"show", Show)
            .AddIf($"dropdown-menu-arrow", Arrow)
            .AddIf($"dropdown-menu-columns dropdown-menu-columns-{Columns}", Columns > 1)
            .ToString();
    }
}