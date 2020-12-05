namespace TabBlazor
{
    public partial class PagePretitle : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("page-pretitle")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}