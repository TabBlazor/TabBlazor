using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    /// <summary>
    /// Defines the corner style of a <see cref="Button"/>.
    /// </summary>
    public enum ButtonShape
    {
        /// <summary>Standard rounded button corners.</summary>
        Default,
        /// <summary>Square (non-rounded) corners.</summary>
        Square,
        /// <summary>Fully rounded pill-shaped corners.</summary>
        Pill
    }

    /// <summary>
    /// Defines the size of a <see cref="Button"/>.
    /// </summary>
    public enum ButtonSize
    {
        /// <summary>Standard button size.</summary>
        Default,
        /// <summary>Larger button.</summary>
        Large,
        /// <summary>Smaller button.</summary>
        Small
    }

    /// <summary>
    /// Defines the rendered HTML element and behavior of a <see cref="Button"/>.
    /// </summary>
    public enum ButtonType
    {
        /// <summary>Renders an anchor (&lt;a&gt;) element, typically used with <see cref="Button.LinkTo"/>.</summary>
        Link,
        /// <summary>Renders a standard &lt;button&gt; element.</summary>
        Button,
        /// <summary>Renders an &lt;input type="button"&gt; element.</summary>
        Input,
        /// <summary>Renders an &lt;input type="submit"&gt; element that submits the enclosing form.</summary>
        Submit,
        /// <summary>Renders an &lt;input type="reset"&gt; element that resets the enclosing form.</summary>
        Reset
    }

    /// <summary>
    /// A clickable button that can render as a button, input, or link. Supports colors, sizes, shapes,
    /// icon-only and loading states.
    /// </summary>
    public partial class Button : TablerBaseComponent
    {
        /// <summary>
        /// The text label shown on the button. Ignored when <see cref="TablerBaseComponent.ChildContent"/> is set.
        /// </summary>
        [Parameter] public string Text { get; set; }
        /// <summary>
        /// When <c>true</c>, the button is disabled and cannot be clicked. Defaults to <c>false</c>.
        /// </summary>
        [Parameter] public bool Disabled { get; set; }
        /// <summary>
        /// When <c>true</c>, the button stretches to the full width of its container. Defaults to <c>false</c>.
        /// </summary>
        [Parameter] public bool Block { get; set; }
        /// <summary>
        /// When <c>true</c>, renders the button as an icon-only button (square, sized for a single icon). Defaults to <c>false</c>.
        /// </summary>
        [Parameter] public bool IsIcon { get; set; }
        /// <summary>
        /// When <c>true</c>, shows a loading spinner on the button. Defaults to <c>false</c>.
        /// </summary>
        [Parameter] public bool IsLoading { get; set; }
        /// <summary>
        /// When <c>true</c>, renders the button as a dropdown toggle (adds a caret). Defaults to <c>false</c>.
        /// </summary>
        [Parameter] public bool IsDropdown { get; set; }
        /// <summary>
        /// Controls how <see cref="TablerBaseComponent.BackgroundColor"/> is applied (e.g. solid, outline, ghost). Defaults to <see cref="ColorType.Default"/>.
        /// </summary>
        [Parameter] public ColorType BackgroundColorType { get; set; } = ColorType.Default;
        /// <summary>
        /// The corner shape of the button. Defaults to <see cref="ButtonShape.Default"/>.
        /// </summary>
        [Parameter] public ButtonShape Shape { get; set; } = ButtonShape.Default;
        /// <summary>
        /// The size of the button. Defaults to <see cref="ButtonSize.Default"/>.
        /// </summary>
        [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Default;
        /// <summary>
        /// The rendered element and behavior of the button. Defaults to <see cref="ButtonType.Button"/>.
        /// </summary>
        [Parameter] public ButtonType Type { get; set; } = ButtonType.Button;
        /// <summary>
        /// The URL navigated to when <see cref="Type"/> is <see cref="ButtonType.Link"/>.
        /// </summary>
        [Parameter] public string LinkTo { get; set; }

        protected string HtmlTag => Type switch
        {
            ButtonType.Input => "input",
            ButtonType.Link => "a",
            ButtonType.Submit => "input",
            ButtonType.Reset => "input",
            _ => "button"
        };

        protected string InputType => Type switch
        {
            ButtonType.Input => "button",
            ButtonType.Button => "button",
            ButtonType.Submit => "submit",
            ButtonType.Reset => "reset",
            _ => null
        };

        protected string Href => Type == ButtonType.Link
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
                .AddIf("dropdown-toggle", IsDropdown)
                .AddCompare("btn-pill", Shape, ButtonShape.Pill)
                .AddCompare("btn-square", Shape, ButtonShape.Square)
                .AddCompare("btn-lg", Size, ButtonSize.Large)
                .AddCompare("btn-sm", Size, ButtonSize.Small)
                .ToString();
    }
}