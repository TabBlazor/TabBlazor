using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public partial class TablerDropDown : TablerBaseComponent
    {
       
        protected override string ClassNames => ClassBuilder
            .Add("dropdown")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}