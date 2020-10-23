namespace Tabler.Components
{
    public partial class TablerDropDownItem : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("dropdown-item")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}