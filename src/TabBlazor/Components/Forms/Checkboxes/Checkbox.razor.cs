using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace TabBlazor
{
    public partial class Checkbox : ComponentBase
    {
        [Parameter] public string Label { get; set; }
        [Parameter] public string Description { get; set; }
        [Parameter] public bool Value { get; set; }
        [Parameter] public EventCallback<bool> ValueChanged { get; set; }
        [Parameter] public EventCallback Changed { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool Switch { get; set; }

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
