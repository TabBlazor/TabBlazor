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
    public string Add { get; init; } = "Add";
    public string Edit { get; init; } = "Edit";
    public string Delete { get; init; } = "Delete";
    public string Details { get; init; } = "Details";
    public string Save { get; init; } = "Save";
    public string Cancel { get; init; } = "Cancel";
    public string Close { get; init; } = "Close";
    public string Back { get; init; } = "Back";
    public string Ok { get; init; } = "OK";
    public string MoreActions { get; init; } = "More actions";
    public string NewItem { get; init; } = "New item";
    public string DiscardChanges { get; init; } = "Discard changes?";
    public string DiscardChangesDetails { get; init; } = "Your changes have not been saved.";
    public string Discard { get; init; } = "Discard";
    public string ConfirmDelete { get; init; } = "Delete this item?";
    public string SaveFailed { get; init; } = "Could not save. Please try again.";
    public string DeleteFailed { get; init; } = "Could not delete. Please try again.";
    public string ActionFailed { get; init; } = "The action failed.";
    public string SavedNotShown { get; init; } = "Saved. The item is not shown with the current search and filters.";
    public string SelectRow { get; init; } = "Select row";
    public string SelectAll { get; init; } = "Select all on this page";
    public string ClearSelection { get; init; } = "Clear selection";
    public string ExpandRow { get; init; } = "Show details";
    public string SelectAnItem { get; init; } = "Select an item to see its details.";

    /// <summary>"{count} selected". Argument: the number of selected rows.</summary>
    public string SelectedCount { get; init; } = "{0} selected";

    /// <summary>"Select all {total} matching". Argument: the total number of matching rows.</summary>
    public string SelectAllMatching { get; init; } = "Select all {0} matching";

    /// <summary>"All {total} matching rows are selected". Argument: the total, or empty when unknown.</summary>
    public string AllMatchingSelected { get; init; } = "All {0} matching rows are selected";

    /// <summary>"Delete {count} items?". Argument: the number of rows.</summary>
    public string ConfirmDeleteSelected { get; init; } = "Delete {0} items?";

    /// <summary>"Showing {first}–{last} of {total}". Arguments: first, last, total.</summary>
    public string ShowingOfTotal { get; init; } = "Showing {0}–{1} of {2}";

    /// <summary>"{count} rows" while virtualized. Argument: the number of rows.</summary>
    public string RowCount { get; init; } = "{0} rows";

    /// <summary>"Showing {first}–{last}" when the total is unknown. Arguments: first, last.</summary>
    public string Showing { get; init; } = "Showing {0}–{1}";
}
