namespace Tabler.Components
{
    public partial class TablerCardTabs : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("card-tabs")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}