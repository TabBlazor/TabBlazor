namespace Tabler.Components
{
    public partial class TablerPageHeader : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("page-header")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}