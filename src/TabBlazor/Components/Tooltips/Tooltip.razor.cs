using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using TabBlazor.Services;

namespace TabBlazor;

public partial class Tooltip
{
    /// <summary>
    /// Custom content rendered inside the tooltip. Takes precedence over <see cref="Text"/> when set.
    /// </summary>
    [Parameter] public RenderFragment TooltipTemplate { get; set; }

    /// <summary>
    /// Plain text shown in the tooltip when <see cref="TooltipTemplate"/> is not set.
    /// </summary>
    [Parameter] public string Text { get; set; }

    /// <summary>
    /// Positioning strategy. When omitted, falls back to <c>TablerOptions.DefaultPositioning</c>.
    /// Any value other than <see cref="TabBlazor.Positioning.Default"/> uses Popper for placement.
    /// </summary>
    [Parameter] public Positioning? Positioning { get; set; }

    /// <summary>
    /// Placement of the tooltip relative to its trigger when Popper is used. Defaults to <see cref="Placement.Top"/>.
    /// </summary>
    [Parameter] public Placement Placement { get; set; } = Placement.Top;

    /// <summary>
    /// Distance in pixels between the tooltip and its trigger when Popper is used. Defaults to 8.
    /// </summary>
    [Parameter] public int Offset { get; set; } = 8;

    private ElementReference referenceEl;
    private ElementReference popperEl;
    private IPopperService popperService;
    private IPopperInstance popperInstance;

    private Positioning EffectivePositioning =>
        Positioning ?? Options.CurrentValue.DefaultPositioning;

    private bool UsePopper => EffectivePositioning != TabBlazor.Positioning.Default;

    protected override void OnInitialized()
    {
        if (UsePopper)
        {
            popperService = (IPopperService)ServiceProvider.GetService(typeof(IPopperService))
                ?? throw new InvalidOperationException(
                    "Popper not registered. Set TablerOptions.EnablePopper = true in AddTabBlazor.");
        }
    }

    private async Task OnMouseOverAsync()
    {
        if (!UsePopper) return;
        if (popperInstance == null)
        {
            popperInstance = await popperService.CreateAsync(referenceEl, popperEl, new PopperOptions
            {
                Placement = Placement,
                Strategy = EffectivePositioning,
                Offset = Offset
            });
        }
        await popperInstance.ShowAsync();
    }

    private async Task OnMouseOutAsync()
    {
        if (!UsePopper || popperInstance == null) return;
        await popperInstance.HideAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (popperInstance != null)
        {
            await popperInstance.DisposeAsync();
        }
    }
}
