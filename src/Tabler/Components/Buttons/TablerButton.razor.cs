using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public enum TablerButtonShape
    {
        Default,
        Square,
        Pill
    }

    public enum TablerButtonSize
    {
        Default,
        Large,
        Small
    }

    public enum TablerButtonType
    {
        Link,
        Button,
        Input,
        Submit,
        Reset
    }

    public partial class TablerButton : TablerBaseComponent
    {
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool Block { get; set; }
        [Parameter] public bool IsIcon { get; set; }
        [Parameter] public bool IsLoading { get; set; }
        [Parameter] public TablerColorType BackgroundColorType { get; set; } = TablerColorType.Default;
        [Parameter] public TablerButtonShape Shape { get; set; } = TablerButtonShape.Default;
        [Parameter] public TablerButtonSize Size { get; set; } = TablerButtonSize.Default;
        [Parameter] public TablerButtonType Type { get; set; } = TablerButtonType.Button;
        [Parameter] public string LinkTo { get; set; }
        [Parameter] public TablerDropDownMenu DropDownMenu { get; set; }

        protected string HtmlTag => Type switch
        {
            TablerButtonType.Input => "input",
            TablerButtonType.Link => "a",
            TablerButtonType.Submit => "input",
            TablerButtonType.Reset => "input",
            _ => "button"
        };

        protected string InputType => Type switch
        {
            TablerButtonType.Input => "button",
            TablerButtonType.Button => "button",
            TablerButtonType.Submit => "submit",
            TablerButtonType.Reset => "reset",
            _ => null
        };

        protected string Href => Type == TablerButtonType.Link
            ? LinkTo
            : null;

        protected override string ClassNames => ClassBuilder
                .Add("btn")
                .Add(BackgroundColor.GetColorClass("btn", BackgroundColorType))
                .Add(TextColor.GetColorClass("text"))
                .AddIf("disabled", Disabled)
                .AddIf("btn-block", Block)
                .AddIf("btn-icon", IsIcon)
                .AddIf("btn-loading", IsLoading)
                .AddCompare("btn-pill", Shape, TablerButtonShape.Pill)
                .AddCompare("btn-square", Shape, TablerButtonShape.Square)
                .AddCompare("btn-lg", Size, TablerButtonSize.Large)
                .AddCompare("btn-sm", Size, TablerButtonSize.Small)
                .ToString();
    }
}