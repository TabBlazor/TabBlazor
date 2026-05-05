using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace TabBlazor.Services;

internal sealed class PopperService : IPopperService, IAsyncDisposable
{
    private readonly IJSRuntime js;
    private readonly TablerOptions options;
    private Task<IJSObjectReference> moduleTask;

    public PopperService(IJSRuntime js, IOptions<TablerOptions> options)
    {
        this.js = js;
        this.options = options.Value;
    }

    private Task<IJSObjectReference> GetModuleAsync()
    {
        return moduleTask ??= js.InvokeAsync<IJSObjectReference>(
            "import", "./_content/TabBlazor/js/popper-interop.js").AsTask();
    }

    public async Task<IPopperInstance> CreateAsync(ElementReference reference, ElementReference popper, PopperOptions opts)
    {
        var module = await GetModuleAsync();
        var jsOpts = new
        {
            placement = opts.Placement.ToPopperString(),
            strategy = opts.Strategy == Positioning.Fixed ? "fixed" : "absolute",
            offset = opts.Offset
        };
        var instance = await module.InvokeAsync<IJSObjectReference>(
            "create", reference, popper, jsOpts, options.PopperScriptUrl);
        return new PopperInstance(instance);
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask != null)
        {
            try
            {
                var module = await moduleTask;
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
    }
}

internal sealed class PopperInstance : IPopperInstance
{
    private readonly IJSObjectReference js;

    public PopperInstance(IJSObjectReference js) => this.js = js;

    public async Task UpdateAsync()
    {
        try { await js.InvokeVoidAsync("update"); }
        catch (JSDisconnectedException) { }
    }

    public async Task ShowAsync()
    {
        try { await js.InvokeVoidAsync("show"); }
        catch (JSDisconnectedException) { }
    }

    public async Task HideAsync()
    {
        try { await js.InvokeVoidAsync("hide"); }
        catch (JSDisconnectedException) { }
    }

    public async Task SetPlacementAsync(Placement placement)
    {
        try { await js.InvokeVoidAsync("setPlacement", placement.ToPopperString()); }
        catch (JSDisconnectedException) { }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await js.InvokeVoidAsync("destroy");
            await js.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
    }
}
