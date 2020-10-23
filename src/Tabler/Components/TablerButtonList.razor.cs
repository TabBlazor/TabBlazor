namespace Tabler.Components
{
    public partial class TablerButtonList : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("btn-list")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}