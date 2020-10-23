using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public partial class TablerInput : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            //.Add("card-tabs")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}