#nullable enable
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace TabBlazor;

public partial class DataView<TItem>
{
    private const int ExportBatchSize = 500;

    private CancellationTokenSource? exportCts;
    private DataExportScope exportScope;
    private int exportedRows;
    private int lastReportedRows;

    /// <summary>
    /// Exports the rows the user picked. The callback gets a <see cref="DataExportContext{TItem}"/>: which rows,
    /// under what filters, in which columns. Setting it shows Export in the "⋮" menu and in the selection bar.
    /// </summary>
    [Parameter] public Func<DataExportContext<TItem>, Task>? OnExport { get; set; }

    /// <summary>
    /// The scopes offered in the Export menu. Defaults to all of them; Selected only shows while rows are
    /// selected.
    /// </summary>
    [Parameter] public DataExportScope[]? ExportScopes { get; set; }

    /// <summary>
    /// The most rows an export may read. Reaching it throws, so a truncated file is never written. Defaults to
    /// 10 000.
    /// </summary>
    [Parameter] public int MaxExportRows { get; set; } = 10_000;

    /// <summary>True while an export is running.</summary>
    public bool IsExporting => exportCts != null;

    /// <summary>The number of rows handed to the export so far.</summary>
    public int ExportedRows => exportedRows;

    internal bool ExportVisible => OnExport != null;

    internal DataExportScope ExportingScope => exportScope;

    /// <summary>The columns an export covers: the visible ones, in display order, minus the non-exportable.</summary>
    internal IReadOnlyList<IDataViewColumn<TItem>> ExportColumns =>
        VisibleColumns.Where(c => c.Exportable).ToList();

    internal IReadOnlyList<DataExportScope> AvailableExportScopes
    {
        get
        {
            var offered = ExportScopes ?? [DataExportScope.AllMatching, DataExportScope.CurrentPage, DataExportScope.Selected];
            return offered.Where(s => s != DataExportScope.Selected || SelectedCount > 0).Distinct().ToList();
        }
    }

    /// <summary>
    /// Runs the <see cref="OnExport"/> callback for a scope. Does nothing when no callback is set or an export is
    /// already running.
    /// </summary>
    public async Task ExportAsync(DataExportScope scope = DataExportScope.AllMatching)
    {
        if (OnExport == null || IsExporting)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        exportCts = cts;
        exportScope = scope;
        exportedRows = 0;
        lastReportedRows = 0;
        StateHasChanged();

        try
        {
            var selection = scope == DataExportScope.Selected ? GetSelection() : null;
            var context = new DataExportContext<TItem>(
                scope,
                BuildExportRequest(scope),
                ExportColumns,
                selection?.Keys,
                engine.TotalCount,
                cts.Token,
                new Progress<int>(ReportExported),
                token => GetExportItemsAsync(scope, token));

            await OnExport(context);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // The user cancelled; nothing to report.
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DataView export ({Scope}) failed.", scope);
            ShowNotice($"{Texts.ExportFailed} {ex.Message}", TablerColor.Danger);
        }
        finally
        {
            exportCts = null;
            cts.Dispose();
            if (!disposed)
            {
                StateHasChanged();
            }
        }
    }

    /// <summary>Cancels the running export.</summary>
    public void CancelExport() => exportCts?.Cancel();

    private DataRequest BuildExportRequest(DataExportScope scope)
    {
        if (scope == DataExportScope.CurrentPage)
        {
            return engine.BuildRequest();
        }

        return engine.BuildRequest(PageRequest.Offset(0, engine.TotalCount ?? MaxExportRows));
    }

    private void ReportExported(int rows)
    {
        exportedRows = rows;

        // One re-render per batch, not per row.
        if (rows - lastReportedRows < 100 && rows != 0)
        {
            return;
        }

        lastReportedRows = rows;
        if (!disposed)
        {
            _ = InvokeAsync(StateHasChanged);
        }
    }

    private async IAsyncEnumerable<TItem> GetExportItemsAsync(
        DataExportScope scope,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (scope == DataExportScope.CurrentPage)
        {
            foreach (var item in engine.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }

            yield break;
        }

        if (scope == DataExportScope.Selected && !allMatchingSelected)
        {
            // The selected rows are already in hand, keyed; no round trip.
            foreach (var item in GetSelection().Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }

            yield break;
        }

        await foreach (var item in PageEveryMatchingAsync(cancellationToken))
        {
            yield return item;
        }
    }

    private async IAsyncEnumerable<TItem> PageEveryMatchingAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // All matching rows means from the first page, not from the page on screen.
        string? cursor = null;
        var batch = Math.Max(engine.PageSize, ExportBatchSize);
        var skip = 0;
        var sent = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // One row past the limit, so reaching it is told apart from ending exactly on it.
            var take = Math.Min(batch, MaxExportRows - sent + 1);
            var request = engine.PagingMode == PagingMode.Cursor
                ? PageRequest.Cursor(cursor, take)
                : PageRequest.Offset(skip, take);

            var page = await engine.FetchPageAsync(request, cancellationToken);
            if (page.Items.Count == 0)
            {
                yield break;
            }

            foreach (var item in page.Items)
            {
                if (sent == MaxExportRows)
                {
                    throw new InvalidOperationException(string.Format(Texts.ExportLimitReached, MaxExportRows));
                }

                yield return item;
                sent++;
            }

            ReportExported(sent);

            if (engine.PagingMode == PagingMode.Cursor)
            {
                cursor = page.NextCursor;
                if (cursor == null)
                {
                    yield break;
                }
            }
            else
            {
                skip += page.Items.Count;

                // A source may cap the page size, so a short page only ends the export when no total says otherwise.
                var more = page.TotalCount is { } total ? skip < total : page.Items.Count >= take;
                if (!more)
                {
                    yield break;
                }
            }
        }
    }
}
