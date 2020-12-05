namespace TabBlazor
{
    public partial class ButtonList : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("btn-list")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}