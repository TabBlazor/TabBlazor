namespace TabBlazor
{
    /// <summary>
    /// Main content area of a <see cref="Card"/>. Place inside a <see cref="Card"/>.
    /// </summary>
    public partial class CardBody : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("card-body")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}