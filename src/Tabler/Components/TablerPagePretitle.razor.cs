namespace Tabler.Components
{
    public partial class TablerPagePretitle : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("page-pretitle")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}