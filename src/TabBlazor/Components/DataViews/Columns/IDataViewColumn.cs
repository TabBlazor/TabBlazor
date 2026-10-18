#nullable enable
namespace TabBlazor;

/// <summary>
/// A column as seen by DataView's engine and views, independent of the column's value type.
/// Implemented by <c>DataColumn&lt;TItem, TValue&gt;</c>.
/// </summary>
public interface IDataViewColumn<TItem>
{
    /// <summary>The column description sent to sources. Valid after the view has collected its columns.</summary>
    DataColumnInfo Info { get; }

    /// <summary>The column's stable id (same as <see cref="DataColumnInfo.Id"/>).</summary>
    string Id => Info.Id;

    /// <summary>The header text.</summary>
    string Title { get; }

    RenderFragment? HeaderTemplate { get; }
    bool Sortable { get; }
    bool Searchable { get; }
    SortOrder? DefaultSort { get; }
    bool Visible { get; }

    /// <summary>Renders the cell content for an item (template, or the formatted value).</summary>
    void RenderCell(RenderTreeBuilder builder, TItem item);

    /// <summary>The formatted value as text, or null.</summary>
    string? GetDisplayText(TItem item);

    string? GetCellClass(TItem item);
    string? GetCellStyle(TItem item);

    /// <summary>The tooltip of a cell (the full text when the column truncates).</summary>
    string? GetCellTitle(TItem item);

    string? GetHeaderClass();
    string? GetHeaderStyle();

    /// <summary>Resolves <see cref="Info"/>. <paramref name="position"/> gives a fallback id for columns without a derivable id.</summary>
    DataColumnInfo Resolve(int position);
}
