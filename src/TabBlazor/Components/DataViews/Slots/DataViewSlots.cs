#nullable enable
namespace TabBlazor;

/// <summary>The template slots a DataView's content can fill.</summary>
public enum DataViewSlot
{
    Header,
    Toolbar,
    Loading,
    Empty,
    NoResults,
    Error,
    ListItem
}

/// <summary>Implemented by the context a DataView cascades to its content; slots register their templates with it.</summary>
public interface IDataViewSlotHost
{
    /// <summary>Sets a slot's template; null clears it.</summary>
    void SetSlot(DataViewSlot slot, object? template);
}

/// <summary>
/// Base for DataView template slots. Slots are placed among the columns, in any order, and register their
/// template with the view while it collects its content (so no <c>&lt;ChildContent&gt;</c> wrapper is needed).
/// </summary>
public abstract class DataViewSlotBase<TTemplate> : ComponentBase, IDisposable where TTemplate : class
{
    [CascadingParameter] private IDataViewSlotHost? Host { get; set; }

    [Parameter, EditorRequired] public TTemplate? ChildContent { get; set; }

    protected abstract DataViewSlot Slot { get; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Host == null)
        {
            throw new InvalidOperationException($"{GetType().Name} must be placed inside a DataView.");
        }

        Host.SetSlot(Slot, ChildContent);
    }

    public void Dispose() => Host?.SetSlot(Slot, null);
}

/// <summary>Custom title markup on the left of the DataView header (replaces <c>Title</c>).</summary>
public sealed class HeaderTemplate : DataViewSlotBase<RenderFragment>
{
    protected override DataViewSlot Slot => DataViewSlot.Header;
}

/// <summary>Your own buttons, placed first in the DataView toolbar.</summary>
public sealed class ToolbarTemplate : DataViewSlotBase<RenderFragment>
{
    protected override DataViewSlot Slot => DataViewSlot.Toolbar;
}

/// <summary>Shown on the first load instead of placeholder rows.</summary>
public sealed class LoadingTemplate : DataViewSlotBase<RenderFragment>
{
    protected override DataViewSlot Slot => DataViewSlot.Loading;
}

/// <summary>Shown when there is no data at all.</summary>
public sealed class EmptyTemplate : DataViewSlotBase<RenderFragment>
{
    protected override DataViewSlot Slot => DataViewSlot.Empty;
}

/// <summary>Shown when no rows match the search or filters.</summary>
public sealed class NoResultsTemplate : DataViewSlotBase<RenderFragment>
{
    protected override DataViewSlot Slot => DataViewSlot.NoResults;
}

/// <summary>Shown when loading failed; the context is the exception.</summary>
public sealed class ErrorTemplate : DataViewSlotBase<RenderFragment<Exception>>
{
    protected override DataViewSlot Slot => DataViewSlot.Error;
}

/// <summary>The content of each item in the list view; the context is the item.</summary>
public sealed class ListItemTemplate<TItem> : DataViewSlotBase<RenderFragment<TItem>>
{
    protected override DataViewSlot Slot => DataViewSlot.ListItem;
}
