using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using TabBlazor.Services;

namespace TabBlazor;

/// <summary>
/// Pours a short shower of confetti over the page when its content is clicked, for a finished setup,
/// a first order or a signup. Use <see cref="TablerService.ConfettiAsync"/> to burst from code instead.
/// </summary>
public partial class Confetti : TablerBaseComponent, IAsyncDisposable
{
    [Inject] private TablerService TablerService { get; set; }

    /// <summary>Wrapping element. Defaults to <c>"span"</c>.</summary>
    [Parameter] public string Tag { get; set; } = "span";
    /// <summary>Number of pieces in the burst. Defaults to 220.</summary>
    [Parameter] public int Count { get; set; } = 220;
    /// <summary>How long pieces keep pouring, in milliseconds. Defaults to 3500.</summary>
    [Parameter] public int Duration { get; set; } = 3500;
    /// <summary>Fall speed multiplier. Defaults to 1.</summary>
    [Parameter] public double Speed { get; set; } = 1;
    /// <summary>Piece colors as CSS colors. When null, the Tabler palette is used.</summary>
    [Parameter] public IReadOnlyList<string> Colors { get; set; }
    /// <summary>When <c>true</c>, clicks do nothing. Defaults to <c>false</c>.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Raised when a burst starts.</summary>
    [Parameter] public EventCallback OnStart { get; set; }
    /// <summary>Raised when the last piece of a burst has landed.</summary>
    [Parameter] public EventCallback OnEnd { get; set; }

    private DotNetObjectReference<Confetti> selfReference;
    private int? activeBurstId;

    protected override string ClassNames => ClassBuilder
        .Add("d-inline-block")
        .AddIf("cursor-pointer", !Disabled)
        .ToString();

    /// <summary>Starts a burst with the component's settings.</summary>
    public async Task Burst()
    {
        if (Disabled)
        {
            return;
        }

        await OnClick.InvokeAsync();
        await OnStart.InvokeAsync();
        selfReference ??= DotNetObjectReference.Create(this);
        activeBurstId = await TablerService.ConfettiAsync(BuildOptions(), selfReference);
    }

    /// <summary>Stops pouring; pieces already in the air keep falling.</summary>
    public async Task Stop()
    {
        if (activeBurstId is { } id)
        {
            activeBurstId = null;
            await TablerService.StopConfettiAsync(id);
        }
    }

    [JSInvokable]
    public Task OnConfettiEnd()
    {
        activeBurstId = null;
        return OnEnd.InvokeAsync();
    }

    private ConfettiOptions BuildOptions() => new()
    {
        Count = Count,
        Duration = Duration,
        Speed = Speed,
        Colors = Colors
    };

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Stop();
        }
        catch (JSDisconnectedException)
        {
        }

        selfReference?.Dispose();
    }
}
