using Microsoft.AspNetCore.Components;

namespace TabBlazor.Services;

public interface IPopperService
{
    Task<IPopperInstance> CreateAsync(ElementReference reference, ElementReference popper, PopperOptions options);
}

public class PopperOptions
{
    public Placement Placement { get; set; } = Placement.Top;
    public Positioning Strategy { get; set; } = Positioning.Absolute;
    public int Offset { get; set; } = 0;
}

public interface IPopperInstance : IAsyncDisposable
{
    Task UpdateAsync();
    Task ShowAsync();
    Task HideAsync();
    Task SetPlacementAsync(Placement placement);
}
