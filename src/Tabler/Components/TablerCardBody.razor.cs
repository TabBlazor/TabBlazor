namespace Tabler.Components
{
    public partial class TablerCardBody : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("card-body")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}