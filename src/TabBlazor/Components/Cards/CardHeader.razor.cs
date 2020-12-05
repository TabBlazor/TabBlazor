namespace TabBlazor
{
    public partial class CardHeader : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("card-header")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}