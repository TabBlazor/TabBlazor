namespace TabBlazor
{
    public partial class PageHeader : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("page-header")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}