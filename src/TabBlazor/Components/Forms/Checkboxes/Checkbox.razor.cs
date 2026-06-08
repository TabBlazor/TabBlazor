using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace TabBlazor
{
    /// <summary>
    /// A checkbox input that can also render as a toggle switch. Supports two-way binding via <c>@bind-Value</c>.
    /// </summary>
    public partial class Checkbox : ComponentBase
    {
        /// <summary>The label text shown next to the checkbox.</summary>
        [Parameter] public string Label { get; set; }
        /// <summary>Optional secondary description text shown below the label.</summary>
        [Parameter] public string Description { get; set; }
        /// <summary>The checked state. Supports two-way binding via <c>@bind-Value</c>.</summary>
        [Parameter] public bool Value { get; set; }
        /// <summary>Raised when the checked state changes.</summary>
        [Parameter] public EventCallback<bool> ValueChanged { get; set; }
        /// <summary>Raised after the checked state changes, in addition to <see cref="ValueChanged"/>.</summary>
        [Parameter] public EventCallback Changed { get; set; }
        /// <summary>When true, the checkbox is disabled. Defaults to false.</summary>
        [Parameter] public bool Disabled { get; set; }
        /// <summary>When true, renders as a toggle switch instead of a checkbox. Defaults to false.</summary>
        [Parameter] public bool Switch { get; set; }

        /// <summary>Optional content rendered inside a select-group item layout instead of the standard label.</summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        private bool isSelectGroup => ChildContent != null;

        protected async Task ToggleState()
        {
            Value = !Value;
            await ValueChanged.InvokeAsync(Value);
            await Changed.InvokeAsync();
        }

        protected string ClassNames => new ClassBuilder()
        .AddIf("form-check", !isSelectGroup)
        .AddIf("form-switch", Switch)
        .AddIf("form-selectgroup-item", isSelectGroup)
        .ToString();

        protected string inputCss => new ClassBuilder()
        .AddIf("form-check-input cursor-pointer", !isSelectGroup)
        .AddIf("form-selectgroup-input", isSelectGroup)

     .ToString();

    }
}
