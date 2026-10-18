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

    /// <summary>
    /// Global defaults for <c>Tabs</c> (preload, keep-alive, URL history).
    /// Parameters set on a component take precedence.
    /// </summary>
    public TabsOptions Tabs { get; set; } = new();

    /// <summary>
    /// App-wide UI strings for <c>DataView</c>. A view's <c>Text</c> parameter takes precedence.
    /// </summary>
    public DataViewText DataViewText { get; set; } = DataViewText.Default;

    /// <summary>
    /// A resource type (for example a marker class next to <c>DataViewText.sv.resx</c>) whose strings translate
    /// <c>DataView</c> through <c>IStringLocalizerFactory</c> (<c>services.AddLocalization()</c>). The keys are the
    /// <see cref="TabBlazor.DataViewText"/> property names; missing keys fall back to <see cref="DataViewText"/>.
    /// </summary>
    public Type DataViewTextResource { get; set; }
}