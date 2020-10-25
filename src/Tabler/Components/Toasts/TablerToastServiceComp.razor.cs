using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public partial class TablerToastServiceComp : ComponentBase
    {
        [Inject] public TablerToastService TablerToastService { get; set; }

        protected override void OnInitialized()
        {
            TablerToastService.OnChanged += OnToastChanged;
        }

        public async Task OnToastChanged()
        {
            await InvokeAsync(() => { StateHasChanged(); });
        }

        public void Dispose()
        {
            TablerToastService.OnChanged -= OnToastChanged;
        }
    }
}