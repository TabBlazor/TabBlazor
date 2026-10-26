#nullable enable
namespace TabBlazor;

/// <summary>
/// Collects declared actions in markup order. Actions are registered while alive (from initialization to
/// disposal); their order comes from the latest render pass, which a marker rendered after them commits. Actions
/// whose parameters did not change are not rendered again, so they keep their previous place.
/// </summary>
public abstract class DataActionContainer<TAction> : ComponentBase, IDisposable where TAction : class
{
    private readonly List<TAction> alive = [];
    private readonly List<TAction> collecting = [];
    private bool isCollecting;
    private bool registered;
    private bool disposed;

    [CascadingParameter] private IDataViewSlotHost? Host { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The committed actions, in markup order.</summary>
    public IReadOnlyList<TAction> Actions { get; private set; } = [];

    protected abstract DataViewSlot Slot { get; }

    internal void Register(TAction action)
    {
        if (!alive.Contains(action))
        {
            alive.Add(action);
        }
    }

    internal void Add(TAction action)
    {
        Register(action);
        if (isCollecting && !collecting.Contains(action))
        {
            collecting.Add(action);
        }
    }

    internal void Remove(TAction action)
    {
        alive.Remove(action);
        collecting.Remove(action);
        if (!disposed && Actions.Contains(action))
        {
            Actions = Actions.Where(a => a != action).ToList();
            Host?.SetSlot(Slot, this);
        }
    }

    // Rendered actions take their markup order; the others keep their previous place.
    private List<TAction> MergeOrder()
    {
        if (alive.All(collecting.Contains))
        {
            return collecting.Where(alive.Contains).ToList();
        }

        var order = Actions.Where(alive.Contains).ToList();
        TAction? previous = null;
        foreach (var action in collecting.Where(alive.Contains))
        {
            if (!order.Contains(action))
            {
                var index = previous == null ? 0 : order.IndexOf(previous) + 1;
                order.Insert(index, action);
            }

            previous = action;
        }

        foreach (var action in alive.Where(a => !order.Contains(a)))
        {
            order.Add(action);
        }

        return order;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Host == null)
        {
            throw new InvalidOperationException($"{GetType().Name} must be placed inside a DataView.");
        }

        collecting.Clear();
        isCollecting = true;
        builder.OpenComponent<CascadingValue<DataActionContainer<TAction>>>(0);
        builder.AddComponentParameter(1, "Value", this);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(b =>
        {
            b.AddContent(4, ChildContent);
            b.OpenComponent<DataActionsCommit>(5);
            b.AddComponentParameter(6, nameof(DataActionsCommit.OnCommit), (Action)Commit);
            b.CloseComponent();
        }));
        builder.CloseComponent();
    }

    // Tells the view only when the list changed, so a re-render of the view cannot loop.
    private void Commit()
    {
        isCollecting = false;
        var order = MergeOrder();
        if (registered && order.SequenceEqual(Actions))
        {
            return;
        }

        registered = true;
        Actions = order;
        Host?.SetSlot(Slot, this);
    }

    // Removed from the markup: the view goes back to its built-in actions.
    public void Dispose()
    {
        disposed = true;
        if (registered)
        {
            Host?.SetSlot(Slot, null);
        }
    }
}

/// <summary>Marks the end of an action container's children.</summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class DataActionsCommit : ComponentBase
{
    [Parameter] public Action? OnCommit { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => OnCommit?.Invoke();
}

/// <summary>
/// Declares a DataView's row actions: icon buttons for the first few, then a "⋯" menu. Place <see cref="RowAction{TItem}"/>,
/// <see cref="EditAction{TItem}"/>, <see cref="DeleteAction{TItem}"/> and <see cref="DetailsAction{TItem}"/> inside.
/// </summary>
public sealed class RowActions<TItem> : DataActionContainer<DataRowActionBase<TItem>>
{
    /// <summary>
    /// How many actions show as icon buttons. With more actions, the first <c>MaxVisible - 1</c> stay inline and
    /// the rest move to the "⋯" menu. Defaults to 3.
    /// </summary>
    [Parameter] public int MaxVisible { get; set; } = 3;

    protected override DataViewSlot Slot => DataViewSlot.RowActions;
}

/// <summary>Base for row actions.</summary>
public abstract class DataRowActionBase<TItem> : ComponentBase, IDisposable
{
    [CascadingParameter] private DataActionContainer<DataRowActionBase<TItem>>? Container { get; set; }

    /// <summary>The tooltip and accessible name.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>The icon.</summary>
    [Parameter] public IIconType? Icon { get; set; }

    /// <summary>Shows the action only for some rows.</summary>
    [Parameter] public Func<TItem, bool>? Visible { get; set; }

    /// <summary>Disables the action for some rows.</summary>
    [Parameter] public Func<TItem, bool>? Disabled { get; set; }

    /// <summary>A question to confirm before the action runs, for example "Archive this order?".</summary>
    [Parameter] public string? Confirm { get; set; }

    /// <summary>Styles the action as dangerous.</summary>
    [Parameter] public bool Danger { get; set; }

    internal string Key { get; } = Guid.CreateVersion7().ToString("N");

    internal abstract string GetTitle(DataViewText texts);

    internal abstract IIconType GetIcon();

    internal virtual bool IsVisible(DataView<TItem> view, TItem item) => Visible?.Invoke(item) ?? true;

    internal virtual bool IsDisabled(DataView<TItem> view, TItem item) => Disabled?.Invoke(item) ?? false;

    internal virtual string? GetConfirm(DataViewText texts) => Confirm;

    internal virtual string? GetHref(TItem item) => null;

    internal abstract Task ExecuteAsync(DataView<TItem> view, TItem item);

    protected override void OnInitialized() => Container?.Register(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder) => Container?.Add(this);

    public void Dispose() => Container?.Remove(this);
}

/// <summary>A custom row action: <c>OnClick</c> (async, with a busy spinner) or <c>Href</c>.</summary>
public sealed class RowAction<TItem> : DataRowActionBase<TItem>
{
    /// <summary>Runs when the action is clicked. Errors are shown, not swallowed.</summary>
    [Parameter] public Func<TItem, Task>? OnClick { get; set; }

    /// <summary>Makes the action a link.</summary>
    [Parameter] public Func<TItem, string>? Href { get; set; }

    internal override string GetTitle(DataViewText texts) => Title ?? string.Empty;

    internal override IIconType GetIcon() => Icon ?? InternalIcons.Dots;

    internal override string? GetHref(TItem item) => Href?.Invoke(item);

    internal override Task ExecuteAsync(DataView<TItem> view, TItem item) => OnClick?.Invoke(item) ?? Task.CompletedTask;
}

/// <summary>Starts editing the row (shown when the view has a <c>SaveItem</c> handler and <c>AllowEdit</c> allows it).</summary>
public sealed class EditAction<TItem> : DataRowActionBase<TItem>
{
    internal override string GetTitle(DataViewText texts) => Title ?? texts.Edit;

    internal override IIconType GetIcon() => Icon ?? InternalIcons.Edit;

    internal override bool IsVisible(DataView<TItem> view, TItem item) => view.CanEdit(item) && base.IsVisible(view, item);

    internal override Task ExecuteAsync(DataView<TItem> view, TItem item) => view.EditAsync(item);
}

/// <summary>Deletes the row after a confirmation (shown when the view has a <c>DeleteItem</c> handler and <c>AllowDelete</c> allows it).</summary>
public sealed class DeleteAction<TItem> : DataRowActionBase<TItem>
{
    public DeleteAction()
    {
        Danger = true;
    }

    internal override string GetTitle(DataViewText texts) => Title ?? texts.Delete;

    internal override IIconType GetIcon() => Icon ?? InternalIcons.Trash;

    internal override bool IsVisible(DataView<TItem> view, TItem item) => view.CanDelete(item) && base.IsVisible(view, item);

    // The view asks for confirmation itself.
    internal override string? GetConfirm(DataViewText texts) => null;

    internal override Task ExecuteAsync(DataView<TItem> view, TItem item) => view.DeleteAsync(item);
}

/// <summary>Opens or closes the row's details.</summary>
public sealed class DetailsAction<TItem> : DataRowActionBase<TItem>
{
    internal override string GetTitle(DataViewText texts) => Title ?? texts.Details;

    internal override IIconType GetIcon() => Icon ?? InternalIcons.Chevron_right;

    internal override bool IsVisible(DataView<TItem> view, TItem item) => view.HasDetails && base.IsVisible(view, item);

    internal override Task ExecuteAsync(DataView<TItem> view, TItem item) => view.ToggleDetailsAsync(item);
}

/// <summary>Built-in edit and delete actions, used when no <see cref="RowActions{TItem}"/> are declared.</summary>
internal sealed class AutoRowAction<TItem>(bool delete) : DataRowActionBase<TItem>
{
    public bool IsDelete { get; } = delete;

    internal override string GetTitle(DataViewText texts) => IsDelete ? texts.Delete : texts.Edit;

    internal override IIconType GetIcon() => IsDelete ? InternalIcons.Trash : InternalIcons.Edit;

    internal override bool IsVisible(DataView<TItem> view, TItem item) => IsDelete ? view.CanDelete(item) : view.CanEdit(item);

    internal override Task ExecuteAsync(DataView<TItem> view, TItem item) => IsDelete ? view.DeleteAsync(item) : view.EditAsync(item);
}

/// <summary>Declares actions for the selected rows, shown in the selection bar.</summary>
public sealed class BulkActions<TItem> : DataActionContainer<DataBulkActionBase<TItem>>
{
    protected override DataViewSlot Slot => DataViewSlot.BulkActions;
}

/// <summary>Base for bulk actions.</summary>
public abstract class DataBulkActionBase<TItem> : ComponentBase, IDisposable
{
    [CascadingParameter] private DataActionContainer<DataBulkActionBase<TItem>>? Container { get; set; }

    [Parameter] public string? Title { get; set; }
    [Parameter] public IIconType? Icon { get; set; }
    [Parameter] public string? Confirm { get; set; }
    [Parameter] public bool Danger { get; set; }

    internal abstract string GetTitle(DataViewText texts);

    internal virtual bool IsVisible(DataView<TItem> view) => true;

    internal abstract Task ExecuteAsync(DataView<TItem> view, DataSelection<TItem> selection);

    protected override void OnInitialized() => Container?.Register(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder) => Container?.Add(this);

    public void Dispose() => Container?.Remove(this);
}

/// <summary>A custom bulk action. With "select all matching", the selection carries the current request instead of items.</summary>
public sealed class BulkAction<TItem> : DataBulkActionBase<TItem>
{
    [Parameter] public Func<DataSelection<TItem>, Task>? OnClick { get; set; }

    internal override string GetTitle(DataViewText texts) => Title ?? string.Empty;

    internal override Task ExecuteAsync(DataView<TItem> view, DataSelection<TItem> selection) =>
        OnClick?.Invoke(selection) ?? Task.CompletedTask;
}

/// <summary>Deletes the selected rows with the view's <c>DeleteItems</c> handler, after a confirmation.</summary>
public sealed class BulkDeleteAction<TItem> : DataBulkActionBase<TItem>
{
    public BulkDeleteAction()
    {
        Danger = true;
    }

    internal override string GetTitle(DataViewText texts) => Title ?? texts.Delete;

    internal override bool IsVisible(DataView<TItem> view) => view.DeleteItems != null;

    internal override Task ExecuteAsync(DataView<TItem> view, DataSelection<TItem> selection) => view.DeleteSelectedAsync();
}
