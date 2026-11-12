#nullable enable
using Microsoft.JSInterop;

namespace TabBlazor;

/// <summary>The edge a column stays at while a DataView table scrolls sideways.</summary>
public enum ColumnFreeze
{
    /// <summary>The column scrolls with the table.</summary>
    None,
    /// <summary>The column stays at the start edge (left, or right in right-to-left layouts).</summary>
    Start,
    /// <summary>The column stays at the end edge (right, or left in right-to-left layouts).</summary>
    End
}

/// <summary>The sticky class and offset of one table cell; empty for a cell that scrolls.</summary>
internal readonly record struct FrozenCell(string? Class, string? Style)
{
    public string? AddClass(string? other) => Class == null ? other : other == null ? Class : $"{Class} {other}";

    public string? AddStyle(string? other) =>
        Style == null ? other : string.IsNullOrWhiteSpace(other) ? Style : $"{other.TrimEnd().TrimEnd(';')};{Style}";
}

/// <summary>Where each cell of a table row sticks: the select, expand and actions cells, then one entry per column.</summary>
internal sealed class DataFreezePlan
{
    public static readonly DataFreezePlan Empty = new([], default, default, default, default, string.Empty);

    public DataFreezePlan(IReadOnlyList<FrozenCell> columns, FrozenCell select, FrozenCell expand,
        FrozenCell startActions, FrozenCell endActions, string signature)
    {
        Columns = columns;
        Select = select;
        Expand = expand;
        StartActions = startActions;
        EndActions = endActions;
        Signature = signature;
    }

    public IReadOnlyList<FrozenCell> Columns { get; }
    public FrozenCell Select { get; }
    public FrozenCell Expand { get; }
    public FrozenCell StartActions { get; }
    public FrozenCell EndActions { get; }

    /// <summary>Changes whenever any cell's freeze changes, so rows know to render again.</summary>
    public string Signature { get; }

    public bool Any => Signature.Contains('S') || Signature.Contains('E');

    public FrozenCell Column(int index) => index < Columns.Count ? Columns[index] : default;

    /// <summary>
    /// Builds the plan for cells in display order. Start cells are offset by the widths of the start cells before them,
    /// end cells by the end cells after them; JS measures those widths into <c>--dv-fs-N</c> / <c>--dv-fe-N</c>.
    /// </summary>
    public static DataFreezePlan Build(IReadOnlyList<ColumnFreeze> cells)
    {
        var lastStart = -1;
        var firstEnd = -1;
        var endCount = 0;
        for (var i = 0; i < cells.Count; i++)
        {
            if (cells[i] == ColumnFreeze.Start)
            {
                lastStart = i;
            }
            else if (cells[i] == ColumnFreeze.End)
            {
                endCount++;
                if (firstEnd < 0)
                {
                    firstEnd = i;
                }
            }
        }

        if (lastStart < 0 && firstEnd < 0)
        {
            return new DataFreezePlan(new FrozenCell[cells.Count], default, default, default, default,
                new string('-', cells.Count));
        }

        var result = new FrozenCell[cells.Count];
        var signature = new char[cells.Count];
        var startIndex = 0;
        var endIndex = endCount;
        for (var i = 0; i < cells.Count; i++)
        {
            switch (cells[i])
            {
                case ColumnFreeze.Start:
                    result[i] = new FrozenCell(i == lastStart ? "dv-fs dv-fs-edge" : "dv-fs",
                        $"inset-inline-start: var(--dv-fs-{startIndex++}, 0px)");
                    signature[i] = 'S';
                    break;
                case ColumnFreeze.End:
                    result[i] = new FrozenCell(i == firstEnd ? "dv-fe dv-fe-edge" : "dv-fe",
                        $"inset-inline-end: var(--dv-fe-{--endIndex}, 0px)");
                    signature[i] = 'E';
                    break;
                default:
                    signature[i] = '-';
                    break;
            }
        }

        return new DataFreezePlan(result, default, default, default, default, new string(signature));
    }

    public DataFreezePlan WithSpecialCells(int selectAt, int expandAt, int startActionsAt, int endActionsAt, int firstColumnAt, int columnCount)
    {
        var all = Columns;
        FrozenCell At(int index) => index >= 0 ? all[index] : default;
        var columns = new FrozenCell[columnCount];
        for (var i = 0; i < columnCount; i++)
        {
            columns[i] = all[firstColumnAt + i];
        }

        return new DataFreezePlan(columns, At(selectAt), At(expandAt), At(startActionsAt), At(endActionsAt), Signature);
    }
}

public partial class DataView<TItem>
{
    private string? freezeSignature;
    private bool frozenCellsSynced;

    /// <summary>The freeze of each cell in the last rendered table.</summary>
    internal DataFreezePlan FreezePlan { get; private set; } = DataFreezePlan.Empty;

    /// <summary>
    /// Works out which cells stick for the columns about to render. Called once per table render; rows read
    /// <see cref="FreezePlan"/>, and when it changes they render again.
    /// </summary>
    internal DataFreezePlan UpdateFreezePlan(IReadOnlyList<IDataViewColumn<TItem>> columns)
    {
        // Stacked cards do not scroll sideways, so nothing sticks there.
        var stacked = IsSmallScreen && EffectiveMobileLayout == DataMobileLayout.Stacked;
        var freezes = columns.Select(c => stacked ? ColumnFreeze.None : c.Freeze).ToArray();

        // In the small-screen scroll layout the first column stays visible, unless columns are frozen to the start already.
        if (IsSmallScreen && EffectiveMobileLayout == DataMobileLayout.Scroll && freezes.Length > 0 &&
            !freezes.Contains(ColumnFreeze.Start))
        {
            freezes[0] = ColumnFreeze.Start;
        }

        var hasStart = freezes.Contains(ColumnFreeze.Start);
        var hasEnd = freezes.Contains(ColumnFreeze.End);
        var actionsAtStart = ShowActionsColumn && RowActionsPosition == RowActionsPosition.Start;
        var actionsAtEnd = ShowActionsColumn && RowActionsPosition == RowActionsPosition.End;

        // Cells before a start-frozen cell, or after an end-frozen one, have to stick too, or they would slide under it.
        var freezeActions = FreezeRowActions && !stacked;
        var leading = hasStart || (actionsAtStart && freezeActions) ? ColumnFreeze.Start : ColumnFreeze.None;
        var trailing = hasEnd || (actionsAtEnd && freezeActions) ? ColumnFreeze.End : ColumnFreeze.None;

        var cells = new List<ColumnFreeze>(freezes.Length + 3);
        var selectAt = ShowCheckboxes ? Add(leading) : -1;
        var expandAt = ShowExpandColumn ? Add(leading) : -1;
        var startActionsAt = actionsAtStart ? Add(leading) : -1;
        var firstColumnAt = cells.Count;
        cells.AddRange(freezes);
        var endActionsAt = actionsAtEnd ? Add(trailing) : -1;

        var plan = DataFreezePlan.Build(cells)
            .WithSpecialCells(selectAt, expandAt, startActionsAt, endActionsAt, firstColumnAt, freezes.Length);

        if (plan.Signature != freezeSignature)
        {
            freezeSignature = plan.Signature;
            engine.InvalidateRows();
        }

        FreezePlan = plan;
        return plan;

        int Add(ColumnFreeze freeze)
        {
            cells.Add(freeze);
            return cells.Count - 1;
        }
    }

    // Measures the frozen cells after each render, and keeps measuring while the table resizes or scrolls.
    private async Task SyncFrozenCellsAsync()
    {
        var active = CurrentView == DataViewKind.Table && FreezePlan.Any && ScrollContainer.Context != null;
        if (!active && !frozenCellsSynced)
        {
            return;
        }

        try
        {
            await JS.InvokeVoidAsync("tabBlazor.syncFrozenColumns", ScrollContainer, active);
            frozenCellsSynced = active;
        }
        catch (JSDisconnectedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}
