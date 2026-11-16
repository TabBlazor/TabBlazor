#nullable enable
namespace TabBlazor;

/// <summary>Which rows an export covers.</summary>
public enum DataExportScope
{
    /// <summary>The rows of the page on screen.</summary>
    CurrentPage,

    /// <summary>The selected rows. With "all matching" selected, every matching row.</summary>
    Selected,

    /// <summary>Every row matching the current search and filters, paged from the source.</summary>
    AllMatching
}

/// <summary>
/// What a <c>DataView</c> hands to <c>OnExport</c>: which rows, under what filters, in which columns. The view
/// answers that; what to do with them is the app's choice, so no file format is built in.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="GetItemsAsync"/> pages the source as it is enumerated, so the rows match the current search, filters
/// and sort. An app that exports on the server can post <see cref="Request"/> instead and never enumerate, so no
/// extra row crosses the wire.
/// </para>
/// <para>
/// The sequence reads a live source: enumerating it twice queries the source twice. Enumerate it once, or collect
/// the rows yourself.
/// </para>
/// </remarks>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class DataExportContext<TItem>
{
    private readonly Func<CancellationToken, IAsyncEnumerable<TItem>> items;

    internal DataExportContext(
        DataExportScope scope,
        DataRequest request,
        IReadOnlyList<IDataViewColumn<TItem>> columns,
        IReadOnlyList<object>? selectedKeys,
        int? totalCount,
        CancellationToken cancellationToken,
        IProgress<int>? progress,
        Func<CancellationToken, IAsyncEnumerable<TItem>> items)
    {
        Scope = scope;
        Request = request;
        Columns = columns;
        SelectedKeys = selectedKeys;
        TotalCount = totalCount;
        CancellationToken = cancellationToken;
        Progress = progress;
        this.items = items;
    }

    /// <summary>The rows the user asked for.</summary>
    public DataExportScope Scope { get; }

    /// <summary>
    /// The request behind the export: the current search, filters, sort and columns, with a page covering every
    /// matching row (the page on screen for <see cref="DataExportScope.CurrentPage"/>). It serializes to JSON, so
    /// it can be posted to an export endpoint.
    /// </summary>
    public DataRequest Request { get; }

    /// <summary>The exported columns: the visible ones, in display order, minus those with <c>Exportable="false"</c>.</summary>
    public IReadOnlyList<IDataViewColumn<TItem>> Columns { get; }

    /// <summary>The keys of the selected rows for <see cref="DataExportScope.Selected"/>, else null.</summary>
    public IReadOnlyList<object>? SelectedKeys { get; }

    /// <summary>The total number of matching rows, or null when the source does not report it.</summary>
    public int? TotalCount { get; }

    /// <summary>Cancelled when the user cancels the export or the view is disposed.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Reports the number of rows handed out so far; the view shows it while the export runs.</summary>
    public IProgress<int>? Progress { get; }

    /// <summary>
    /// The rows of the export, paged from the source as they are read. Throws when the view's
    /// <c>MaxExportRows</c> is reached, so a partial file is never written.
    /// </summary>
    public IAsyncEnumerable<TItem> GetItemsAsync(CancellationToken cancellationToken = default) =>
        cancellationToken.CanBeCanceled && cancellationToken != CancellationToken
            ? Linked(cancellationToken)
            : items(CancellationToken);

    private async IAsyncEnumerable<TItem> Linked(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        await foreach (var item in items(linked.Token).WithCancellation(linked.Token))
        {
            yield return item;
        }
    }
}
