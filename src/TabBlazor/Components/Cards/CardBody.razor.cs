namespace TabBlazor
{
    public partial class CardBody : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("card-body")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}