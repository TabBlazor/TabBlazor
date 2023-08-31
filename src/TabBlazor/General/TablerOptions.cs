using TabBlazor.Components.Tables;

namespace TabBlazor;

public class TablerOptions
{
    public OnCancelStrategy DefaultOnCancelStrategy { get; set; } = OnCancelStrategy.AsIs;
}