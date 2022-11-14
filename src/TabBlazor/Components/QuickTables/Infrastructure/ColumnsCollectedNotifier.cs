namespace TabBlazor.Components.QuickTables.Infrastructure;

public class ColumnsCollectedNotifier<TGridItem> : IComponent
{
    private bool _isFirstRender = true;

    [CascadingParameter] internal InternalGridContext<TGridItem> InternalGridContext { get; set; } = default!;

    public void Attach(RenderHandle renderHandle)
    {
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        if (_isFirstRender)
        {
            _isFirstRender = false;
            parameters.SetParameterProperties(this);
            return InternalGridContext.ColumnsFirstCollected.InvokeCallbacksAsync(null);
        }

        return Task.CompletedTask;
    }
}