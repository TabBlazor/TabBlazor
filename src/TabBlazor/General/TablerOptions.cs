using System.Reflection;
using TabBlazor.Components.Tables;

namespace TabBlazor;

public class TablerOptions
{
    public OnCancelStrategy DefaultOnCancelStrategy { get; set; } = OnCancelStrategy.AsIs;

    /// <summary>
    /// For now only used when scanning for flags
    /// </summary>
    public Func<List<Assembly>> AssemblyScanFilter { get; set; } = () => AppDomain.CurrentDomain.GetAssemblies().ToList();
}