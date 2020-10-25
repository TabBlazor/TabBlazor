namespace Tabler.Components
{
    public partial class TablerCardHeader : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("card-header")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}