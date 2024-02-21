
namespace TabBlazor
{
    public partial class CardFooter : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("card-footer")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}