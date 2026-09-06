namespace TabBlazor
{
    /// <summary>Wraps buttons in a wrapping flex list with consistent spacing.</summary>
    public partial class ButtonList : TablerBaseComponent
    {
        /// <summary>When <c>true</c>, centers the buttons horizontally. Defaults to <c>false</c>.</summary>
        [Parameter] public bool Centered { get; set; }

        protected override string ClassNames => ClassBuilder
            .Add("btn-list")
            .AddIf("btn-list-center", Centered)
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();
    }
}