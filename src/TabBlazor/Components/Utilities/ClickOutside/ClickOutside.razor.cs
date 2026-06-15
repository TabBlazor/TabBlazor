using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace TabBlazor;

public partial class ClickOutside
{
    private string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Whether the click-outside handler is registered when the component renders or when it is clicked.
    /// Default is <see cref="RegisterStrategy.OnClick"/>.
    /// </summary>
    [Parameter]
    public RegisterStrategy Strategy { get; set; } = RegisterStrategy.OnClick;

    /// <summary>
    /// Whether one or many outside clicks are received. With <see cref="ConcurrenceStrategy.One"/> the
    /// handler is unregistered after the first click outside. Pairing <see cref="RegisterStrategy.OnClick"/>
    /// with <see cref="ConcurrenceStrategy.One"/> reduces load on Blazor. Default is <see cref="ConcurrenceStrategy.One"/>.
    /// </summary>
    [Parameter]
    public ConcurrenceStrategy Concurrence { get; set; } = ConcurrenceStrategy.One;

    /// <summary>
    /// Additional HTML attributes applied to the wrapping element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object> Attributes { get; set; }

    /// <summary>
    /// Invoked when a click outside the component is detected.
    /// </summary>
    [Parameter]
    public EventCallback OnClickOutside { get; set; }

    /// <summary>
    /// The content wrapped by the click-outside detector.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Controls when the click-outside handler is registered.
    /// </summary>
    public enum RegisterStrategy
    {
        /// <summary>Register the handler when the component is clicked.</summary>
        OnClick = 1,
        /// <summary>Register the handler when the component first renders.</summary>
        OnRender = 2
    }

    /// <summary>
    /// Controls how many outside clicks the handler responds to.
    /// </summary>
    public enum ConcurrenceStrategy
    {
        /// <summary>Respond to a single outside click, then unregister.</summary>
        One = 1,
        /// <summary>Respond to outside clicks repeatedly until disposed.</summary>
        Many = 2
    }

    [JSInvokable]
    public async Task InvokeClickOutside()
    {
        await OnClickOutside.InvokeAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender && Strategy == RegisterStrategy.OnRender)
        {
            await JSRuntime.InvokeVoidAsync("tabBlazor.clickOutsideHandler.addEvent", Id, Concurrence == ConcurrenceStrategy.One, DotNetObjectReference.Create(this));
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("tabBlazor.clickOutsideHandler.removeEvent", Id);
        }
        catch (Exception)
        {
        }
    }

    private async Task AddClickOutsideHandler()
    {
        if (Strategy == RegisterStrategy.OnClick)
        {
            await JSRuntime.InvokeVoidAsync("tabBlazor.clickOutsideHandler.addEvent", Id, Concurrence == ConcurrenceStrategy.One, DotNetObjectReference.Create(this));
        }
    }
}
