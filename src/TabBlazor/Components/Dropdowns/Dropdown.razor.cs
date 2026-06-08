using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;
using TabBlazor.Services;

namespace TabBlazor
{
    /// <summary>
    /// A toggleable dropdown menu. Place the trigger in child content and the menu in <see cref="DropdownTemplate"/>.
    /// When <see cref="Positioning"/> requires it, uses Popper (enable via <c>TablerOptions.EnablePopper</c>).
    /// </summary>
    public partial class Dropdown : TablerBaseComponent, IAsyncDisposable
    {
        /// <summary>The dropdown menu content (typically a <c>DropdownMenu</c>).</summary>
        [Parameter] public RenderFragment DropdownTemplate { get; set; }
        /// <summary>When true, the dropdown closes after an item is clicked. Defaults to true.</summary>
        [Parameter] public bool CloseOnClick { get; set; } = true;
        /// <summary>The direction the menu opens.</summary>
        [Parameter] public DropdownDirection Direction { get; set; }
        /// <summary>The direction sub-menus open. Defaults to <see cref="DropdownDirection.End"/>.</summary>
        [Parameter] public DropdownDirection SubMenusDirection { get; set; } = DropdownDirection.End;
        /// <summary>Raised when the dropdown opens or closes.</summary>
        [Parameter] public EventCallback<bool> OnExpanded { get; set; }

        /// <summary>Positioning strategy. When null, uses the configured default.</summary>
        [Parameter] public Positioning? Positioning { get; set; }
        /// <summary>Popper placement of the menu. Defaults to <see cref="Placement.BottomStart"/>.</summary>
        [Parameter] public Placement Placement { get; set; } = Placement.BottomStart;
        /// <summary>Popper offset in pixels. Defaults to 2.</summary>
        [Parameter] public int PopperOffset { get; set; } = 2;

        [Inject] private IServiceProvider ServiceProvider { get; set; }

        private Positioning EffectivePositioning =>
            Positioning ?? Options.CurrentValue.DefaultPositioning;

        /// <summary>True while the dropdown is open.</summary>
        public bool IsExpanded => isExpanded;

        protected bool isExpanded;

        private double top;
        private double left;
        private bool isContextMenu;

        private ElementReference referenceEl;
        private ElementReference popperEl;
        private IPopperService popperService;
        private IPopperInstance popperInstance;

        public bool UsePopper => EffectivePositioning != TabBlazor.Positioning.Default;

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
                    Strategy = EffectivePositioning,
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

        /// <summary>Toggles the dropdown open/closed.</summary>
        public void Toogle()
        {
            SetExpanded(!isExpanded);
        }

        /// <summary>Opens the dropdown.</summary>
        public void Open()
        {
            SetExpanded(true);
        }

        /// <summary>Opens the dropdown as a context menu positioned at the mouse event coordinates.</summary>
        public void OpenAsContextMenu(MouseEventArgs e)
        {
            isContextMenu = true;
            top = e.ClientY;
            left = e.ClientX;
            SetExpanded(true);
            InvokeAsync(StateHasChanged);
        }

        /// <summary>Closes the dropdown.</summary>
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
