#nullable enable
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace TabBlazor;

/// <summary>
/// A column of a <c>DataView</c>. <see cref="Value"/> takes any expression: a member path
/// (<c>o =&gt; o.Customer.Name</c>) or a calculated value (<c>u =&gt; $"{u.FirstName} {u.LastName}"</c>).
/// The column's stable <see cref="Id"/> and the member paths used to sort, search and filter are worked out
/// from it, and can be overridden with <see cref="Id"/>, <see cref="SortBy"/> and <see cref="FilterBy"/>.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public partial class DataColumn<TItem, TValue> : ComponentBase, IDataViewColumn<TItem>
{
    private DataColumnInfo? info;
    private ExpressionKey? infoKey;
    private ExpressionKey? valueKey;
    private Func<TItem, TValue>? getter;
    private string? defaultTitle;

    [CascadingParameter] internal DataViewContext<TItem>? Context { get; set; }

    /// <summary>The displayed value: a member path or any expression.</summary>
    [Parameter] public Expression<Func<TItem, TValue>>? Value { get; set; }

    /// <summary>
    /// The column's stable name, used in requests, the URL and saved state. Optional: a member path gives
    /// <c>customer.name</c>, a calculated value the members it uses joined with '-' (<c>firstName-lastName</c>).
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>The header text. Defaults to the member's <c>[Display(Name)]</c> or name.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Custom header content.</summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>Content of a small menu in the column header, for example extra controls for this column.</summary>
    [Parameter] public RenderFragment? ColumnOptions { get; set; }

    /// <summary>The column can be sorted.</summary>
    [Parameter] public bool Sortable { get; set; }

    /// <summary>The column takes part in the global search.</summary>
    [Parameter] public bool Searchable { get; set; }

    /// <summary>The column can be filtered: a control picked from the value type (or <c>FilterBy</c>) appears in the header and the Filters popup.</summary>
    [Parameter] public bool Filterable { get; set; }

    /// <summary>Limits or extends the filter operators; an operator picker appears when more than one is allowed.</summary>
    [Parameter] public FilterOperator[]? FilterOperators { get; set; }

    /// <summary>Your own filter control; the context can read and set the column's filter.</summary>
    [Parameter] public RenderFragment<DataFilterContext<TItem>>? FilterTemplate { get; set; }

    /// <summary>Sorts by this column when the view loads.</summary>
    [Parameter] public SortOrder? DefaultSort { get; set; }

    /// <summary>A .NET format string for the value, applied with the current culture (<c>N2</c>, <c>yyyy-MM-dd</c>).</summary>
    [Parameter] public string? Format { get; set; }

    /// <summary>Formats the value for display. Takes precedence over <see cref="Format"/>.</summary>
    [Parameter] public Func<TValue, string?>? Formatter { get; set; }

    /// <summary>Cell content. Takes precedence over <see cref="Format"/> and <see cref="Formatter"/>.</summary>
    [Parameter] public RenderFragment<TItem>? Template { get; set; }

    /// <summary>Sort using these members instead of the displayed value: one member, or <c>x =&gt; new { x.A, x.B }</c>.</summary>
    [Parameter] public Expression<Func<TItem, object>>? SortBy { get; set; }

    /// <summary>
    /// Filter and search using these members instead of the displayed value. One member filters exactly as if the
    /// column were bound to it; several (<c>x =&gt; new { x.A, x.B }</c>) match each word against one of them.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? FilterBy { get; set; }

    /// <summary>Custom in-memory search matching: (item, term) =&gt; bool.</summary>
    [Parameter] public Expression<Func<TItem, string, bool>>? SearchExpression { get; set; }

    /// <summary>Show the column. Hidden columns still describe the data (search, sort state).</summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>The column width, for example <c>120px</c> or <c>10%</c>.</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>The minimum column width.</summary>
    [Parameter] public string? MinWidth { get; set; }

    /// <summary>Hides the column below a Bootstrap breakpoint (sm, md, lg, xl, xxl), so narrow screens show the important columns.</summary>
    [Parameter] public string? HideBelow { get; set; }

    /// <summary>Keeps cells on one line.</summary>
    [Parameter] public bool NoWrap { get; set; }

    /// <summary>Cuts long text with "…" and shows the full text on hover.</summary>
    [Parameter] public bool Truncate { get; set; }

    /// <summary>Horizontal alignment of cells and header.</summary>
    [Parameter] public Align Align { get; set; } = Align.Start;

    /// <summary>CSS classes for every cell of the column.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>CSS classes for the header cell.</summary>
    [Parameter] public string? HeaderClass { get; set; }

    /// <summary>A Tabler color per cell, for example <c>o =&gt; o.Total &lt; 0 ? TablerColor.Red : null</c>.</summary>
    [Parameter] public Func<TItem, TablerColor?>? CellColor { get; set; }

    /// <summary>CSS classes per cell.</summary>
    [Parameter] public Func<TItem, string?>? CellClass { get; set; }

    /// <summary>Inline style per cell.</summary>
    [Parameter] public Func<TItem, string?>? CellStyle { get; set; }

    public DataColumnInfo Info => info ?? throw new InvalidOperationException(
        "The column has not been resolved yet; its DataView resolves it when collecting columns.");

    string IDataViewColumn<TItem>.Id => Info.Id;

    RenderFragment? IDataViewColumn<TItem>.ColumnOptions => ColumnOptions;

    public IReadOnlyList<FilterOperator> AllowedFilterOperators =>
        Filterable ? DataFilters.AllowedOperators(Info, FilterOperators) : [];

    string IDataViewColumn<TItem>.Title => Title ?? defaultTitle ?? info?.Id ?? string.Empty;

    protected override void OnParametersSet()
    {
        if (Context == null)
        {
            throw new InvalidOperationException(
                $"{nameof(DataColumn<TItem, TValue>)} must be placed inside a DataView<{typeof(TItem).Name}>.");
        }

        var newValueKey = ExpressionKey.Create(Value);
        if (!Equals(newValueKey, valueKey))
        {
            valueKey = newValueKey;
            getter = Value?.Compile();
            defaultTitle = GetDefaultTitle(Value);
            setter = CompileSetter(Value);
        }

        var newInfoKey = ExpressionKey.Create(Value, SortBy, FilterBy, SearchExpression, Id);
        if (!Equals(newInfoKey, infoKey))
        {
            infoKey = newInfoKey;
            info = null;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Columns render nothing themselves; they register with the view while it collects its columns.
        Context?.AddColumn(this);
    }

    public DataColumnInfo Resolve(int position)
    {
        if (info == null || (Id == null && Value == null))
        {
            info = DataColumnInfoBuilder.Create(Value, Id, SortBy, FilterBy, SearchExpression,
                fallbackId: FallbackId(position));
        }

        return info;
    }

    private string FallbackId(int position)
    {
        if (!string.IsNullOrWhiteSpace(Title))
        {
            var letters = new string(Title.Where(char.IsLetterOrDigit).ToArray());
            if (letters.Length > 0)
            {
                return DataColumnInfoBuilder.ToCamelCase(letters);
            }
        }

        return $"column{position + 1}";
    }

    public void RenderCell(RenderTreeBuilder builder, TItem item)
    {
        // Called from Razor markup: a region keeps these sequence numbers apart from the caller's.
        builder.OpenRegion(1002);
        if (Template != null)
        {
            builder.AddContent(0, Template(item));
        }
        else
        {
            builder.AddContent(1, GetDisplayText(item));
        }

        builder.CloseRegion();
    }

    public string? GetDisplayText(TItem item)
    {
        if (getter == null || !TryGetValue(item, out var value) || value is null)
        {
            return null;
        }

        if (Formatter != null)
        {
            return Formatter(value);
        }

        if (Format != null && value is IFormattable formattable)
        {
            return formattable.ToString(Format, CultureInfo.CurrentCulture);
        }

        return value switch
        {
            Enum e => EnumDisplay.GetName(e),
            IFormattable f => f.ToString(null, CultureInfo.CurrentCulture),
            _ => value.ToString()
        };
    }

    /// <summary>Reads the value; false when a null in the member chain stops it.</summary>
    public bool TryGetValue(TItem item, out TValue? value)
    {
        value = default;
        if (getter == null)
        {
            return false;
        }

        try
        {
            value = getter(item);
            return true;
        }
        catch (NullReferenceException)
        {
            return false;
        }
    }

    public string? GetCellClass(TItem item)
    {
        var builder = new ClassBuilder(Class)
            .Add(HideBelowClass)
            .AddIf("text-end", Align == Align.End)
            .AddIf("text-nowrap", NoWrap)
            .AddIf("text-truncate", Truncate);
        if (CellColor != null)
        {
            builder.Add(DataViewStyles.ColorClass(CellColor(item)));
        }

        if (CellClass != null)
        {
            builder.Add(CellClass(item));
        }

        return NullIfEmpty(builder.ToString());
    }

    public string? GetCellStyle(TItem item)
    {
        var style = CellStyle?.Invoke(item);
        if (Truncate)
        {
            style = $"max-width: {Width ?? "0"}; {style}";
        }

        return NullIfEmpty(style);
    }

    public string? GetCellTitle(TItem item) => Truncate && Template == null ? GetDisplayText(item) : null;

    public string? GetHeaderClass() =>
        NullIfEmpty(new ClassBuilder(HeaderClass).Add(HideBelowClass).AddIf("text-end", Align == Align.End).AddIf("text-nowrap", NoWrap).ToString());

    private string? HideBelowClass => string.IsNullOrWhiteSpace(HideBelow)
        ? null
        : $"d-none d-{DataView<TItem>.NormalizeBreakpoint(HideBelow)}-table-cell";

    public string? GetHeaderStyle()
    {
        var style = string.Empty;
        if (!string.IsNullOrWhiteSpace(Width))
        {
            style += $"width: {Width};";
        }

        if (!string.IsNullOrWhiteSpace(MinWidth))
        {
            style += $"min-width: {MinWidth};";
        }

        return NullIfEmpty(style);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? GetDefaultTitle(LambdaExpression? expression)
    {
        if (expression == null)
        {
            return null;
        }

        var body = DataColumnInfoBuilder.StripConvert(expression.Body);
        if (body is MemberExpression member && DataColumnInfoBuilder.GetMemberPath(expression) != null)
        {
            return member.Member.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? member.Member.Name;
        }

        return null;
    }
}

/// <summary>
/// Identifies expressions across renders: Razor creates new expression trees on every render, so they are
/// compared by text and by the closure objects they capture.
/// </summary>
internal sealed class ExpressionKey : IEquatable<ExpressionKey>
{
    private readonly string text;
    private readonly object?[] constants;

    private ExpressionKey(string text, object?[] constants)
    {
        this.text = text;
        this.constants = constants;
    }

    public static ExpressionKey Create(params object?[] parts)
    {
        var text = new System.Text.StringBuilder();
        var constants = new List<object?>();
        foreach (var part in parts)
        {
            text.Append('|');
            if (part is Expression expression)
            {
                text.Append(expression);
                var collector = new ConstantCollector();
                collector.Visit(expression);
                constants.AddRange(collector.Constants);
            }
            else
            {
                text.Append(part);
            }
        }

        return new ExpressionKey(text.ToString(), constants.ToArray());
    }

    public bool Equals(ExpressionKey? other) =>
        other != null && text == other.text && constants.Length == other.constants.Length &&
        constants.Zip(other.constants).All(p => ReferenceEquals(p.First, p.Second) || Equals(p.First, p.Second));

    public override bool Equals(object? obj) => Equals(obj as ExpressionKey);

    public override int GetHashCode() => text.GetHashCode();

    private sealed class ConstantCollector : ExpressionVisitor
    {
        public List<object?> Constants { get; } = [];

        protected override Expression VisitConstant(ConstantExpression node)
        {
            Constants.Add(node.Value);
            return node;
        }
    }
}
