#nullable enable
namespace TabBlazor;

/// <summary>How rows can be selected.</summary>
public enum DataSelectionMode
{
    None,
    /// <summary>One row at a time, by clicking it.</summary>
    Single,
    /// <summary>Checkboxes, a "select all" checkbox and a selection bar with bulk actions.</summary>
    Multiple
}

/// <summary>What clicking a row does.</summary>
public enum DataRowClick
{
    /// <summary>Navigate when <c>RowHref</c> is set, else open details when a <c>DetailsTemplate</c> exists, else select in single selection mode.</summary>
    Auto,
    None,
    Details,
    Select,
    Navigate
}

/// <summary>Where details appear.</summary>
public enum DataDetailsMode
{
    /// <summary>Expands under the row or list item.</summary>
    Inline,
    /// <summary>An offcanvas panel from the right; the view stays usable and another item swaps the content.</summary>
    Drawer,
    /// <summary>A panel beside the view showing the current item (Outlook style).</summary>
    SidePanel
}

/// <summary>Where the row actions column sits.</summary>
public enum RowActionsPosition
{
    End,
    Start
}

/// <summary>
/// The selected rows, for bulk actions. With <see cref="AllMatching"/>, every row matching the current search and
/// filters is meant: use <see cref="Request"/> to act on them on the server instead of downloading them.
/// </summary>
public sealed record DataSelection<TItem>(
    IReadOnlyList<TItem> Items,
    IReadOnlyList<object> Keys,
    bool AllMatching,
    DataRequest? Request,
    int? TotalCount)
{
    /// <summary>The number of selected rows (the total when all matching rows are selected and the total is known).</summary>
    public int Count => AllMatching && TotalCount is { } total ? total : Keys.Count;
}

/// <summary>
/// A row and what can be done with it, for <c>RowActionsTemplate</c>, <c>EditingActionsTemplate</c> and custom
/// templates. <see cref="Save"/> runs the normal validation and save flow.
/// </summary>
public sealed class DataRowContext<TItem>
{
    private readonly DataView<TItem> view;

    internal DataRowContext(DataView<TItem> view, TItem item)
    {
        this.view = view;
        Item = item;
    }

    public TItem Item { get; }
    public bool IsEditing => view.IsEditingRow(Item);
    public bool IsSaving => IsEditing && view.EditSession?.IsSaving == true;
    public bool IsSelected => view.IsSelected(Item);
    public bool IsExpanded => view.IsExpanded(Item);
    public bool IsCurrent => view.IsCurrent(Item);
    public bool CanEdit => view.CanEdit(Item);
    public bool CanDelete => view.CanDelete(Item);

    public Task Edit() => view.EditAsync(Item);
    public Task<bool> Save() => view.EditSession is { } session ? view.SaveEditAsync(session) : Task.FromResult(false);
    public Task Cancel() => view.CancelEditAsync(force: false);
    public Task Delete() => view.DeleteAsync(Item);
    public Task ToggleDetails() => view.ToggleDetailsAsync(Item);
    public Task ToggleSelected() => view.ToggleSelectedAsync(Item);
}

/// <summary>The context of a <c>DetailsTemplate</c>.</summary>
public sealed class DataDetailsContext<TItem>
{
    private readonly DataView<TItem> view;

    internal DataDetailsContext(DataView<TItem> view, TItem item, DataDetailsEntry? entry)
    {
        this.view = view;
        Item = item;
        Entry = entry;
    }

    public TItem Item { get; }

    internal DataDetailsEntry? Entry { get; }

    /// <summary>True while <c>LoadDetails</c> runs for this item.</summary>
    public bool IsLoading => Entry?.Task is { IsCompleted: false };

    /// <summary>The error of <c>LoadDetails</c>, or null.</summary>
    public Exception? Error => Entry?.Task is { IsFaulted: true } task ? task.Exception?.GetBaseException() : null;

    /// <summary>The loaded details as <typeparamref name="TDetails"/>, or default while loading or without <c>LoadDetails</c>.</summary>
    public TDetails? Get<TDetails>() =>
        Entry?.Task is Task<TDetails> { IsCompletedSuccessfully: true } task ? task.Result : default;

    public Task Close() => view.CloseDetailsAsync(Item);
    public Task Edit() => view.EditAsync(Item);
    public Task Reload() => view.ReloadDetailsAsync(Item);
}

internal sealed class DataDetailsEntry(Task task)
{
    public Task Task { get; } = task;
}

/// <summary>The state of one row; rows re-render only when it (or their item) changes.</summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public readonly record struct DataRowState(
    bool Selected,
    bool Editing,
    bool Saving,
    bool Expanded,
    bool Current,
    string? BusyAction,
    bool JustSaved);
