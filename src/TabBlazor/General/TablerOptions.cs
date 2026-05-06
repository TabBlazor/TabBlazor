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

    /// <summary>
    /// Enables popper.js-based dynamic positioning. When true, IPopperService is registered
    /// and the popper script is loaded lazily on first use.
    /// </summary>
    public bool EnablePopper { get; set; }

    /// <summary>
    /// URL of the popper.js UMD script. Loaded once on first use of IPopperService.
    /// </summary>
    public string PopperScriptUrl { get; set; } = "https://unpkg.com/@popperjs/core@2";

    /// <summary>
    /// Default Positioning for components that don't specify one
    /// (Dropdown, Autocomplete, ItemSelect, Typeahead, Tooltip).
    /// Setting this to anything other than <see cref="Positioning.Default"/> implies
    /// <see cref="EnablePopper"/> = true.
    /// </summary>
    public Positioning DefaultPositioning { get; set; } = Positioning.Default;
}