#nullable enable
namespace TabBlazor;

/// <summary>
/// The built-in UI strings of a <c>DataView</c>, with English defaults. Override individual strings per view
/// with <c>Text="@(new DataViewText { Search = "Sök…" })"</c>.
/// </summary>
public sealed record DataViewText
{
    public static DataViewText Default { get; } = new();

    public string Search { get; init; } = "Search…";
    public string Loading { get; init; } = "Loading…";
    public string NoData { get; init; } = "No data";
    public string NoResults { get; init; } = "No results";
    public string ClearFilters { get; init; } = "Clear filters";
    public string LoadFailed { get; init; } = "Could not load data.";
    public string Retry { get; init; } = "Retry";
    public string Refresh { get; init; } = "Refresh";
    public string MoreOptions { get; init; } = "More options";
    public string Density { get; init; } = "Density";
    public string DensityCompact { get; init; } = "Compact";
    public string DensityNormal { get; init; } = "Normal";
    public string DensityComfortable { get; init; } = "Comfortable";
    public string RowsPerPage { get; init; } = "Rows:";
    public string FirstPage { get; init; } = "First page";
    public string PreviousPage { get; init; } = "Previous page";
    public string NextPage { get; init; } = "Next page";
    public string LastPage { get; init; } = "Last page";
    public string OpenRow { get; init; } = "Open";
    public string SortBy { get; init; } = "Sort by";
    public string TableView { get; init; } = "Table";
    public string ListView { get; init; } = "List";

    /// <summary>"Showing {first}–{last} of {total}". Arguments: first, last, total.</summary>
    public string ShowingOfTotal { get; init; } = "Showing {0}–{1} of {2}";

    /// <summary>"Showing {first}–{last}" when the total is unknown. Arguments: first, last.</summary>
    public string Showing { get; init; } = "Showing {0}–{1}";
}
