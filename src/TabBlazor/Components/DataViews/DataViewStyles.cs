#nullable enable
namespace TabBlazor;

/// <summary>Row density of a DataView table.</summary>
public enum TableDensity
{
    Compact,
    Normal,
    Comfortable
}

internal static class DataViewStyles
{
    /// <summary>
    /// The class for a row or cell color: Bootstrap's contextual table classes for theme colors
    /// (<c>table-danger</c>), light backgrounds for palette colors (<c>bg-red-lt</c>). Both follow the theme.
    /// </summary>
    public static string? ColorClass(TablerColor? color, bool table = true) => color switch
    {
        null or TablerColor.Default => null,
        _ when !table => $"bg-{color.Value.ToString().ToLowerInvariant()}-lt",
        TablerColor.Primary or TablerColor.Secondary or TablerColor.Success or TablerColor.Info or
            TablerColor.Warning or TablerColor.Danger or TablerColor.Light or TablerColor.Dark =>
            $"table-{color.Value.ToString().ToLowerInvariant()}",
        _ => $"bg-{color.Value.ToString().ToLowerInvariant()}-lt"
    };

    public static string StateClass(DataViewStatus status) => status switch
    {
        DataViewStatus.Error => "dv-error",
        DataViewStatus.NoResults => "dv-no-results",
        DataViewStatus.Empty => "dv-empty",
        _ => "dv-loading-row"
    };
}
