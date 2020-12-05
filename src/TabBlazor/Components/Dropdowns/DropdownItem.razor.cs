namespace TabBlazor.Components
{
    public partial class DropdownItem : TablerBaseComponent
    {
        protected override string ClassNames => ClassBuilder
            .Add("dropdown-item")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}