#nullable enable
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TabBlazor.Services;

namespace TabBlazor;

/// <summary>A message shown at the top of a DataView (a failed action, a saved item that is filtered out).</summary>
internal sealed record DataViewNotice(string Message, TablerColor Color);

public partial class DataView<TItem>
{
    private static readonly MethodInfo MemberwiseCloneMethod =
        typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly Dictionary<object, TItem> selected = [];
    private readonly HashSet<object> expanded = [];
    private readonly Dictionary<object, DataDetailsEntry> detailsCache = [];
    private readonly Dictionary<object, string> busyActions = [];
    private readonly HashSet<object> justSaved = [];
    private IReadOnlyList<TItem>? lastSelectedItemsParameter;
    private bool allMatchingSelected;
    private object? editKey;
    private object? currentKey;
    private TItem? currentItem;
    private string? lastSearchForSelection;
    private IReadOnlyList<FilterSpec> lastFiltersForSelection = [];

    [Inject] private IModalService ModalService { get; set; } = default!;

    // Selection

    /// <summary>How rows are selected. Defaults to none.</summary>
    [Parameter] public DataSelectionMode SelectionMode { get; set; }

    /// <summary>The selected rows. Use <c>@bind-SelectedItems</c>. Selection is tracked by key, so it survives reloads and paging.</summary>
    [Parameter] public IReadOnlyList<TItem>? SelectedItems { get; set; }

    [Parameter] public EventCallback<IReadOnlyList<TItem>> SelectedItemsChanged { get; set; }

    /// <summary>Raised when the selection changes, with keys and the "all matching" state.</summary>
    [Parameter] public EventCallback<DataSelection<TItem>> OnSelectionChanged { get; set; }

    /// <summary>What clicking a row does. Defaults to <see cref="DataRowClick.Auto"/>.</summary>
    [Parameter] public DataRowClick RowClick { get; set; }

    // Editing

    /// <summary>Saves an edited or new item. Having a handler turns editing on.</summary>
    [Parameter] public Func<DataEditContext<TItem>, Task<EditResult<TItem>>>? SaveItem { get; set; }

    /// <summary>Deletes an item. Having a handler turns delete on.</summary>
    [Parameter] public Func<TItem, Task<EditResult>>? DeleteItem { get; set; }

    /// <summary>Deletes the selected rows (bulk delete).</summary>
    [Parameter] public Func<DataSelection<TItem>, Task<EditResult>>? DeleteItems { get; set; }

    /// <summary>Creates a new item. With <see cref="SaveItem"/>, turns adding on.</summary>
    [Parameter] public Func<TItem>? NewItem { get; set; }

    /// <summary>Copies a row into a draft. Defaults to a shallow copy; use it for records (<c>with</c>), DTOs or deep copies.</summary>
    [Parameter] public Func<TItem, TItem>? EditModelFactory { get; set; }

    /// <summary>How rows are edited. Defaults to inline (popup in views without columns).</summary>
    [Parameter] public DataEditMode EditMode { get; set; } = DataEditMode.Inline;

    /// <summary>Allows editing per row.</summary>
    [Parameter] public Func<TItem, bool>? AllowEdit { get; set; }

    /// <summary>Allows deleting per row.</summary>
    [Parameter] public Func<TItem, bool>? AllowDelete { get; set; }

    /// <summary>Shows the "+ Add" button. Defaults to true when <see cref="SaveItem"/> and <see cref="NewItem"/> are set.</summary>
    [Parameter] public bool? ShowAdd { get; set; }

    /// <summary>Raised (and awaited) before an edit starts; set Cancel to prevent it.</summary>
    [Parameter] public EventCallback<DataEditStartingEventArgs<TItem>> OnEditStarting { get; set; }

    /// <summary>Raised when an edit is canceled, with the original row (default when adding).</summary>
    [Parameter] public EventCallback<TItem> OnEditCanceled { get; set; }

    /// <summary>Raised after an item was saved, with the saved item.</summary>
    [Parameter] public EventCallback<TItem> OnSaved { get; set; }

    /// <summary>With <see cref="DataEditMode.Custom"/>: show your own UI and call the context's SaveAsync or CancelAsync.</summary>
    [Parameter] public Func<DataEditContext<TItem>, Task>? OnCustomEdit { get; set; }

    /// <summary>Replaces the built-in confirmation dialog; returns true to go ahead.</summary>
    [Parameter] public Func<string, Task<bool>>? OnConfirm { get; set; }

    // Actions and details

    /// <summary>Where the row actions column sits. Defaults to the end.</summary>
    [Parameter] public RowActionsPosition RowActionsPosition { get; set; }

    /// <summary>Where details appear. Defaults to inline.</summary>
    [Parameter] public DataDetailsMode DetailsMode { get; set; }

    /// <summary>
    /// Loads extra data when details open, for example <c>o =&gt; api.GetOrderDetailsAsync(o.Id)</c>. Cached by key;
    /// read it in the template with <c>context.Get&lt;OrderDetails&gt;()</c>.
    /// </summary>
    [Parameter] public Func<TItem, Task>? LoadDetails { get; set; }

    /// <summary>Closes the previous inline details when another opens. Defaults to true.</summary>
    [Parameter] public bool SingleExpand { get; set; } = true;

    /// <summary>The title of the details drawer. Defaults to the first column's text.</summary>
    [Parameter] public Func<TItem, string?>? DetailsTitle { get; set; }

    // Slot-filled templates
    internal RowActions<TItem>? RowActionsSlot { get; private set; }
    internal BulkActions<TItem>? BulkActionsSlot { get; private set; }
    internal RenderFragment<DataRowContext<TItem>>? RowActionsTemplate { get; private set; }
    internal RenderFragment<DataRowContext<TItem>>? EditingActionsTemplate { get; private set; }
    internal RenderFragment<DataDetailsContext<TItem>>? DetailsTemplate { get; private set; }

    internal DataEditContext<TItem>? EditSession { get; private set; }
    internal DataViewNotice? Notice { get; private set; }
    internal bool HasDetails => DetailsTemplate != null;
    internal bool CanAdd => SaveItem != null && NewItem != null;
    internal bool AddVisible => ShowAdd ?? CanAdd;
    internal bool ShowCheckboxes => SelectionMode == DataSelectionMode.Multiple;
    internal bool ShowExpandColumn => HasDetails && DetailsMode == DataDetailsMode.Inline && CurrentView == DataViewKind.Table;
    internal bool EditingIsDirty => EditSession?.IsModified == true;

    /// <summary>The edit mode in use: views without columns edit in a popup.</summary>
    internal DataEditMode EffectiveEditMode =>
        EditMode == DataEditMode.Inline && CurrentView != DataViewKind.Table ? DataEditMode.Popup : EditMode;

    internal IReadOnlyList<DataRowActionBase<TItem>> RowActionList
    {
        get
        {
            if (RowActionsSlot != null)
            {
                return RowActionsSlot.Actions;
            }

            var actions = new List<DataRowActionBase<TItem>>(2);
            if (SaveItem != null)
            {
                actions.Add(autoEdit);
            }

            if (DeleteItem != null)
            {
                actions.Add(autoDelete);
            }

            return actions;
        }
    }

    private readonly AutoRowAction<TItem> autoEdit = new(false);
    private readonly AutoRowAction<TItem> autoDelete = new(true);

    internal int MaxVisibleActions => RowActionsSlot?.MaxVisible ?? 3;

    internal bool HasRowActions => RowActionsTemplate != null || RowActionList.Count > 0;

    /// <summary>The table shows an actions column: for row actions, or for Save and Cancel while editing inline.</summary>
    internal bool ShowActionsColumn => HasRowActions || (EditSession != null && EffectiveEditMode == DataEditMode.Inline);

    private void SetInteractionSlot(DataViewSlot slot, object? template)
    {
        switch (slot)
        {
            case DataViewSlot.RowActions:
                RowActionsSlot = (RowActions<TItem>?)template;
                engine.InvalidateRows();
                StateHasChanged();
                break;
            case DataViewSlot.BulkActions:
                BulkActionsSlot = (BulkActions<TItem>?)template;
                StateHasChanged();
                break;
            case DataViewSlot.RowActionsTemplate: RowActionsTemplate = (RenderFragment<DataRowContext<TItem>>?)template; break;
            case DataViewSlot.EditingActionsTemplate: EditingActionsTemplate = (RenderFragment<DataRowContext<TItem>>?)template; break;
            case DataViewSlot.Details: DetailsTemplate = (RenderFragment<DataDetailsContext<TItem>>?)template; break;
        }
    }

    internal DataRowContext<TItem> GetRowContext(TItem item) => new(this, item);

    internal DataRowState GetRowState(TItem item)
    {
        var key = KeyOf(item);
        return new DataRowState(
            IsSelected(item),
            IsEditingRow(item),
            IsEditingRow(item) && EditSession!.IsSaving,
            expanded.Contains(key),
            Equals(currentKey, key),
            busyActions.GetValueOrDefault(key),
            justSaved.Contains(key));
    }

    internal string? GetBusyAction(TItem item) => busyActions.GetValueOrDefault(KeyOf(item));

    private object KeyOf(TItem item) => GetRowKey(item) ?? (object)"<null>";

    private void OnParametersSetInteraction()
    {
        if (!ReferenceEquals(SelectedItems, lastSelectedItemsParameter))
        {
            lastSelectedItemsParameter = SelectedItems;
            selected.Clear();
            allMatchingSelected = false;
            foreach (var item in SelectedItems ?? [])
            {
                selected[KeyOf(item)] = item;
            }
        }
    }

    // Keeps selected, current and saved rows pointing at the latest loaded instances.
    private void OnPageLoadedInteraction(DataPage<TItem> page)
    {
        foreach (var item in page.Items)
        {
            var key = KeyOf(item);
            if (selected.ContainsKey(key))
            {
                selected[key] = item;
            }

            if (Equals(currentKey, key))
            {
                currentItem = item;
            }
        }
    }

    // Called before the user changes paging, sort, search or filters.
    private async Task<bool> CanChangeStateAsync() => await CancelEditAsync(force: false);

    private void OnUserStateChangedInteraction()
    {
        justSaved.Clear();
        var state = engine.GetState();
        if (state.Search != lastSearchForSelection || !state.Filters.SequenceEqual(lastFiltersForSelection))
        {
            allMatchingSelected = false;
        }

        lastSearchForSelection = state.Search;
        lastFiltersForSelection = state.Filters;
    }

    // Selection

    internal bool IsSelected(TItem item) =>
        SelectionMode != DataSelectionMode.None && (allMatchingSelected || selected.ContainsKey(KeyOf(item)));

    internal bool AllMatchingSelected => allMatchingSelected;

    internal int SelectedCount => allMatchingSelected && engine.TotalCount is { } total ? total : selected.Count;

    /// <summary>The keys of the selected rows (also those not loaded).</summary>
    public IReadOnlyCollection<object> GetSelectedKeys() => selected.Keys.ToList();

    /// <summary>The current selection, for bulk actions.</summary>
    public DataSelection<TItem> GetSelection()
    {
        DataRequest? request = null;
        if (allMatchingSelected)
        {
            var current = engine.BuildRequest();
            request = current with { Page = PageRequest.Offset(0, engine.TotalCount ?? int.MaxValue) };
        }

        return new DataSelection<TItem>(selected.Values.ToList(), selected.Keys.ToList(), allMatchingSelected, request, engine.TotalCount);
    }

    /// <summary>Clears the selection.</summary>
    public Task ClearSelectionAsync()
    {
        selected.Clear();
        allMatchingSelected = false;
        return NotifySelectionChangedAsync();
    }

    internal async Task ToggleSelectedAsync(TItem item)
    {
        if (SelectionMode == DataSelectionMode.None)
        {
            return;
        }

        var key = KeyOf(item);
        if (allMatchingSelected)
        {
            // Leaving "all matching": keep the loaded rows except this one.
            allMatchingSelected = false;
            selected.Clear();
            foreach (var row in engine.Items)
            {
                selected[KeyOf(row)] = row;
            }

            selected.Remove(key);
        }
        else if (!selected.Remove(key))
        {
            if (SelectionMode == DataSelectionMode.Single)
            {
                selected.Clear();
            }

            selected[key] = item;
        }

        await NotifySelectionChangedAsync();
    }

    internal bool? PageSelectionState
    {
        get
        {
            if (allMatchingSelected && engine.Items.Count > 0)
            {
                return true;
            }

            var count = engine.Items.Count(i => selected.ContainsKey(KeyOf(i)));
            return count == 0 ? false : count == engine.Items.Count ? true : null;
        }
    }

    internal async Task TogglePageSelectionAsync()
    {
        var selectAll = PageSelectionState != true;
        allMatchingSelected = false;
        foreach (var item in engine.Items)
        {
            if (selectAll)
            {
                selected[KeyOf(item)] = item;
            }
            else
            {
                selected.Remove(KeyOf(item));
            }
        }

        await NotifySelectionChangedAsync();
    }

    internal async Task SelectAllMatchingAsync()
    {
        allMatchingSelected = true;
        foreach (var item in engine.Items)
        {
            selected[KeyOf(item)] = item;
        }

        await NotifySelectionChangedAsync();
    }

    private async Task NotifySelectionChangedAsync()
    {
        var items = selected.Values.ToList();
        lastSelectedItemsParameter = items;
        StateHasChanged();
        await SelectedItemsChanged.InvokeAsync(items);
        await OnSelectionChanged.InvokeAsync(GetSelection());
    }

    // Row clicks

    internal async Task HandleRowClickAsync(TItem item)
    {
        if (OnRowClicked.HasDelegate)
        {
            await OnRowClicked.InvokeAsync(item);
        }

        var click = RowClick;
        if (click == DataRowClick.Auto)
        {
            click = RowHref != null ? DataRowClick.Navigate
                : HasDetails ? DataRowClick.Details
                : SelectionMode == DataSelectionMode.Single ? DataRowClick.Select
                : DataRowClick.None;
        }

        if (IsEditingRow(item))
        {
            return;
        }

        switch (click)
        {
            case DataRowClick.Details when HasDetails:
                await ToggleDetailsAsync(item);
                break;
            case DataRowClick.Select:
                await ToggleSelectedAsync(item);
                break;
        }
    }

    // Row and bulk actions

    internal async Task RunRowActionAsync(DataRowActionBase<TItem> action, TItem item)
    {
        var key = KeyOf(item);
        if (busyActions.ContainsKey(key) || action.IsDisabled(this, item))
        {
            return;
        }

        var confirm = action.GetConfirm(Texts);
        if (confirm != null && !await ConfirmAsync(confirm))
        {
            return;
        }

        busyActions[key] = action.Key;
        StateHasChanged();
        try
        {
            await action.ExecuteAsync(this, item);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DataView row action {Action} failed.", action.GetTitle(Texts));
            ShowNotice($"{Texts.ActionFailed} {ex.Message}", TablerColor.Danger);
        }
        finally
        {
            busyActions.Remove(key);
            StateHasChanged();
        }
    }

    internal async Task RunBulkActionAsync(DataBulkActionBase<TItem> action)
    {
        var confirm = action.Confirm;
        if (confirm != null && !await ConfirmAsync(confirm))
        {
            return;
        }

        try
        {
            await action.ExecuteAsync(this, GetSelection());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DataView bulk action {Action} failed.", action.GetTitle(Texts));
            ShowNotice($"{Texts.ActionFailed} {ex.Message}", TablerColor.Danger);
        }

        StateHasChanged();
    }

    internal void ShowNotice(string message, TablerColor color)
    {
        Notice = new DataViewNotice(message, color);
        StateHasChanged();
    }

    internal void DismissNotice()
    {
        Notice = null;
        StateHasChanged();
    }

    internal async Task<bool> ConfirmAsync(string text, string? details = null)
    {
        if (OnConfirm != null)
        {
            return await OnConfirm(text);
        }

        return await ModalService.ShowDialogAsync(new Components.Modals.DialogOptions
        {
            MainText = text,
            SubText = details,
            OkText = Texts.Ok,
            CancelText = Texts.Cancel,
            IconType = InternalIcons.Alert_triangle,
            StatusColor = TablerColor.Warning
        });
    }

    // Editing

    internal bool CanEdit(TItem item) => SaveItem != null && (AllowEdit?.Invoke(item) ?? true);

    internal bool CanDelete(TItem item) => DeleteItem != null && (AllowDelete?.Invoke(item) ?? true);

    internal bool IsEditingRow(TItem item) =>
        EditSession is { } session &&
        (session.IsNew ? ReferenceEquals(item, session.Draft) : editKey != null && Equals(editKey, KeyOf(item)));

    /// <summary>Starts editing a row (asks first when another edit has unsaved changes).</summary>
    public async Task EditAsync(TItem item)
    {
        if (!CanEdit(item) || IsEditingRow(item) || !await CancelEditAsync(force: false))
        {
            return;
        }

        var args = new DataEditStartingEventArgs<TItem>(item, false);
        await OnEditStarting.InvokeAsync(args);
        if (args.Cancel)
        {
            return;
        }

        var key = KeyOf(item);
        expanded.Remove(key);
        await BeginEditAsync(new DataEditContext<TItem>(this, item, CreateDraft(item), false), key);
    }

    /// <summary>Starts adding a new item (asks first when another edit has unsaved changes).</summary>
    public async Task AddAsync()
    {
        if (!CanAdd || !await CancelEditAsync(force: false))
        {
            return;
        }

        var args = new DataEditStartingEventArgs<TItem>(default, true);
        await OnEditStarting.InvokeAsync(args);
        if (args.Cancel)
        {
            return;
        }

        await BeginEditAsync(new DataEditContext<TItem>(this, default, NewItem!(), true), null);
    }

    private async Task BeginEditAsync(DataEditContext<TItem> session, object? key)
    {
        session.EditContext.SetFieldCssClassProvider(new TabFieldCssClassProvider());
        if (FormValidator is TablerDataAnnotationsValidator or null)
        {
            session.EditContext.EnableDataAnnotationsValidation(Services);
        }

        EditSession = session;
        editKey = key;
        StateHasChanged();
        if (EffectiveEditMode == DataEditMode.Custom && OnCustomEdit != null)
        {
            await OnCustomEdit(session);
        }
    }

    private IFormValidator? FormValidator => Services.GetService<IFormValidator>();

    /// <summary>The validator component to render inside edit UIs, or null when data annotations are enabled directly.</summary>
    internal Type? ValidatorComponent => FormValidator is TablerDataAnnotationsValidator or null ? null : FormValidator.Component;

    private TItem CreateDraft(TItem item)
    {
        if (EditModelFactory != null)
        {
            return EditModelFactory(item);
        }

        return item is null || typeof(TItem).IsValueType ? item : (TItem)MemberwiseCloneMethod.Invoke(item, null)!;
    }

    /// <summary>
    /// Ends the current edit. Unless <paramref name="force"/> is set, asks "Discard changes?" when the draft was
    /// changed. Returns false when the user keeps editing.
    /// </summary>
    internal async Task<bool> CancelEditAsync(bool force)
    {
        var session = EditSession;
        if (session == null)
        {
            return true;
        }

        if (!force && session.IsModified &&
            !await ConfirmAsync(Texts.DiscardChanges, Texts.DiscardChangesDetails))
        {
            return false;
        }

        if (!ReferenceEquals(EditSession, session))
        {
            return true;
        }

        EditSession = null;
        editKey = null;
        StateHasChanged();
        await OnEditCanceled.InvokeAsync(session.Original!);
        return true;
    }

    /// <summary>Validates the draft and runs <see cref="SaveItem"/>; returns true when saved.</summary>
    internal async Task<bool> SaveEditAsync(DataEditContext<TItem> session)
    {
        if (session.IsSaving || !ReferenceEquals(session, EditSession) || SaveItem == null)
        {
            return false;
        }

        session.Message = null;
        session.MessageStore.Clear();
        var validator = FormValidator;
        var valid = validator == null || validator is TablerDataAnnotationsValidator
            ? session.EditContext.Validate()
            : await validator.ValidateAsync(session.ValidatorInstance, session.EditContext);
        if (!valid)
        {
            StateHasChanged();
            return false;
        }

        session.IsSaving = true;
        StateHasChanged();
        EditResult<TItem> result;
        try
        {
            result = await SaveItem(session);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DataView failed to save an item.");
            session.IsSaving = false;
            session.Message = Texts.SaveFailed;
            StateHasChanged();
            return false;
        }

        session.IsSaving = false;
        if (result.Status == EditResultStatus.Failed)
        {
            session.Message = result.Message ?? Texts.SaveFailed;
            StateHasChanged();
            return false;
        }

        if (result.Status == EditResultStatus.Invalid)
        {
            var unmatched = session.ApplyFieldErrors(result.Errors);
            session.Message = result.Message ?? (unmatched.Count > 0 ? string.Join(" ", unmatched) : null);
            StateHasChanged();
            return false;
        }

        var saved = result.Item ?? session.Draft;
        EditSession = null;
        editKey = null;
        ApplySavedToItems(session, saved);
        var savedKey = KeyOf(saved);
        justSaved.Clear();
        justSaved.Add(savedKey);
        await OnSaved.InvokeAsync(saved);

        if (session.IsNew || Items != null)
        {
            await engine.LoadAsync();
            if (session.IsNew && !engine.Items.Any(i => Equals(KeyOf(i), savedKey)))
            {
                ShowNotice(Texts.SavedNotShown, TablerColor.Success);
            }
        }
        else
        {
            engine.ReplaceItem(i => Equals(KeyOf(i), KeyOf(session.Original!)), saved);
        }

        StateHasChanged();
        return true;
    }

    // With local Items, the saved item replaces the original in the list (or is added).
    private void ApplySavedToItems(DataEditContext<TItem> session, TItem saved)
    {
        if (Items is not IList<TItem> { IsReadOnly: false } list)
        {
            return;
        }

        if (session.IsNew)
        {
            if (Items is not Array)
            {
                list.Add(saved);
            }

            return;
        }

        var index = list.IndexOf(session.Original!);
        if (index >= 0)
        {
            list[index] = saved;
        }
    }

    /// <summary>Deletes a row after a confirmation.</summary>
    public async Task DeleteAsync(TItem item)
    {
        if (!CanDelete(item) || !await ConfirmAsync(Texts.ConfirmDelete))
        {
            return;
        }

        EditResult result;
        try
        {
            result = await DeleteItem!(item);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DataView failed to delete an item.");
            ShowNotice(Texts.DeleteFailed, TablerColor.Danger);
            return;
        }

        if (!result.Succeeded)
        {
            ShowNotice(result.Message ?? Texts.DeleteFailed, TablerColor.Danger);
            return;
        }

        var key = KeyOf(item);
        RemoveFromItems([item]);
        selected.Remove(key);
        expanded.Remove(key);
        if (Equals(currentKey, key))
        {
            currentKey = null;
            currentItem = default;
        }

        if (IsEditingRow(item))
        {
            EditSession = null;
            editKey = null;
        }

        await engine.LoadAsync();
    }

    internal async Task DeleteSelectedAsync()
    {
        if (DeleteItems == null)
        {
            return;
        }

        var selection = GetSelection();
        if (selection.Count == 0 ||
            !await ConfirmAsync(string.Format(Texts.ConfirmDeleteSelected, selection.Count)))
        {
            return;
        }

        EditResult result;
        try
        {
            result = await DeleteItems(selection);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DataView failed to delete the selected items.");
            ShowNotice(Texts.DeleteFailed, TablerColor.Danger);
            return;
        }

        if (!result.Succeeded)
        {
            ShowNotice(result.Message ?? Texts.DeleteFailed, TablerColor.Danger);
            return;
        }

        RemoveFromItems(selection.Items);
        selected.Clear();
        allMatchingSelected = false;
        await NotifySelectionChangedAsync();
        await engine.LoadAsync();
    }

    private void RemoveFromItems(IEnumerable<TItem> items)
    {
        if (Items is IList<TItem> { IsReadOnly: false } list and not Array)
        {
            foreach (var item in items.ToList())
            {
                list.Remove(item);
            }
        }
    }

    private async Task OnBeforeInternalNavigationAsync(LocationChangingContext context)
    {
        if (EditingIsDirty && !await ConfirmAsync(Texts.DiscardChanges, Texts.DiscardChangesDetails))
        {
            context.PreventNavigation();
        }
    }

    // Details

    internal bool IsExpanded(TItem item) => expanded.Contains(KeyOf(item));

    internal bool IsCurrent(TItem item) => Equals(currentKey, KeyOf(item));

    internal TItem? CurrentDetailsItem => currentKey == null ? default : currentItem;

    internal bool HasCurrentDetails => currentKey != null;

    internal async Task ToggleDetailsAsync(TItem item)
    {
        if (!HasDetails)
        {
            return;
        }

        var key = KeyOf(item);
        if (DetailsMode == DataDetailsMode.Inline)
        {
            if (!expanded.Remove(key))
            {
                if (SingleExpand)
                {
                    expanded.Clear();
                }

                expanded.Add(key);
                StateHasChanged();
                await EnsureDetailsLoadedAsync(item);
            }
        }
        else if (Equals(currentKey, key) && DetailsMode == DataDetailsMode.Drawer)
        {
            currentKey = null;
            currentItem = default;
        }
        else
        {
            currentKey = key;
            currentItem = item;
            StateHasChanged();
            await EnsureDetailsLoadedAsync(item);
        }

        StateHasChanged();
    }

    internal Task CloseDetailsAsync(TItem item)
    {
        var key = KeyOf(item);
        expanded.Remove(key);
        if (Equals(currentKey, key))
        {
            currentKey = null;
            currentItem = default;
        }

        StateHasChanged();
        return Task.CompletedTask;
    }

    internal Task CloseCurrentDetailsAsync()
    {
        currentKey = null;
        currentItem = default;
        StateHasChanged();
        return Task.CompletedTask;
    }

    internal Task ReloadDetailsAsync(TItem item)
    {
        detailsCache.Remove(KeyOf(item));
        return EnsureDetailsLoadedAsync(item);
    }

    internal DataDetailsContext<TItem> GetDetailsContext(TItem item) =>
        new(this, item, detailsCache.GetValueOrDefault(KeyOf(item)));

    private async Task EnsureDetailsLoadedAsync(TItem item)
    {
        if (LoadDetails == null)
        {
            return;
        }

        var key = KeyOf(item);
        if (detailsCache.ContainsKey(key))
        {
            return;
        }

        var entry = new DataDetailsEntry(LoadDetails(item));
        detailsCache[key] = entry;
        engine.InvalidateRows();
        StateHasChanged();
        try
        {
            await entry.Task;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DataView failed to load details.");
        }

        engine.InvalidateRows();
        StateHasChanged();
    }

    internal string GetDetailsTitle(TItem item) =>
        DetailsTitle?.Invoke(item) ?? VisibleColumns.FirstOrDefault()?.GetDisplayText(item) ?? Texts.Details;
}
