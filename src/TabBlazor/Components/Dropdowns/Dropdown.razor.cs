using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;
using TabBlazor.Services;

namespace TabBlazor
{
    public partial class Dropdown : TablerBaseComponent, IAsyncDisposable
    {
        [Parameter] public RenderFragment DropdownTemplate { get; set; }
        [Parameter] public bool CloseOnClick { get; set; } = true;
        [Parameter] public DropdownDirection Direction { get; set; }
        [Parameter] public DropdownDirection SubMenusDirection { get; set; } = DropdownDirection.End;
        [Parameter] public EventCallback<bool> OnExpanded { get; set; }

        [Parameter] public Positioning Positioning { get; set; } = Positioning.Default;
        [Parameter] public Placement Placement { get; set; } = Placement.BottomStart;
        [Parameter] public int PopperOffset { get; set; } = 2;

        [Inject] private IServiceProvider ServiceProvider { get; set; }

        public bool IsExpanded => isExpanded;

        protected bool isExpanded;

        private double top;
        private double left;
        private bool isContextMenu;

        private ElementReference referenceEl;
        private ElementReference popperEl;
        private IPopperService popperService;
        private IPopperInstance popperInstance;

        public bool UsePopper => Positioning != Positioning.Default;

        protected override string ClassNames => ClassBuilder
            .AddIf("dropdown", Direction == DropdownDirection.Down)
            .AddIf("dropend", Direction == DropdownDirection.End)
            .Add("cursor-pointer")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();

        protected override void OnInitialized()
        {
            if (UsePopper)
            {
                popperService = ServiceProvider.GetService(typeof(IPopperService)) as IPopperService
                    ?? throw new InvalidOperationException(
                        "Popper not registered. Set TablerOptions.EnablePopper = true in AddTabBlazor.");
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!UsePopper) return;

            if (isExpanded && popperInstance == null)
            {
                popperInstance = await popperService.CreateAsync(referenceEl, popperEl, new PopperOptions
                {
                    Placement = Placement,
                    Strategy = Positioning,
                    Offset = PopperOffset
                });
                await popperInstance.ShowAsync();
            }
            else if (!isExpanded && popperInstance != null)
            {
                await popperInstance.DisposeAsync();
                popperInstance = null;
            }
        }

        private void SetExpanded(bool expanded)
        {
            isExpanded = expanded;
            OnExpanded.InvokeAsync(isExpanded);
        }

        protected void OnClickOutside()
        {
            if (isExpanded)
            {
                SetExpanded(false);
            }
        }

        private string GetSyle()
        {
            if (isContextMenu)
            {
                return $"position:fixed;top:{top}px;left:{left}px";
            }

            return "";
        }

        protected async Task OnDropdownClick(MouseEventArgs e)
        {
            await OnClick.InvokeAsync(e);
            Toogle();
        }

        public void Toogle()
        {
            SetExpanded(!isExpanded);
        }

        public void Open()
        {
            SetExpanded(true);
        }

        public void OpenAsContextMenu(MouseEventArgs e)
        {
            isContextMenu = true;
            top = e.ClientY;
            left = e.ClientX;
            SetExpanded(true);
            InvokeAsync(StateHasChanged);
        }

        public void Close()
        {
            SetExpanded(false);
            InvokeAsync(StateHasChanged);
        }

        public async ValueTask DisposeAsync()
        {
            if (popperInstance != null)
            {
                await popperInstance.DisposeAsync();
                popperInstance = null;
            }
        }
    }
}
