using TabBlazor.Services;

namespace TabBlazor
{
    /// <summary>
    /// A three-state checkbox whose <see cref="Value"/> can be true, false, or null (indeterminate).
    /// Supports two-way binding via <c>@bind-Value</c>.
    /// </summary>
    public partial class CheckboxTriState : ComponentBase
    {
        private bool isInitialized;
        [Inject] public TablerService TablerService { get; set; }
        /// <summary>The label text shown next to the checkbox.</summary>
        [Parameter] public string Label { get; set; }
        /// <summary>Optional secondary description text shown below the label.</summary>
        [Parameter] public string Description { get; set; }

        /// <summary>The state: true (checked), false (unchecked), or null (indeterminate).</summary>
        [Parameter] public bool? Value { get; set; }

        /// <summary>Raised when the state changes.</summary>
        [Parameter] public EventCallback<bool?> ValueChanged { get; set; }
        /// <summary>Raised after the state changes, in addition to <see cref="ValueChanged"/>.</summary>
        [Parameter] public EventCallback Changed { get; set; }
        /// <summary>When true, the checkbox is disabled. Defaults to false.</summary>
        [Parameter] public bool Disabled { get; set; }

        protected ElementReference Element { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender || isInitialized)
            {
                isInitialized = true;
                await TablerService.SetElementProperty(Element, "indeterminate", !Value.HasValue);
            }
        }

        protected async Task ToggleState()
        {
            if (Value == null)
            {
                Value = true;
            }
            else
            {
                Value = !Value;
            }

            await ValueChanged.InvokeAsync(Value);
            await Changed.InvokeAsync();
        }
    }
}