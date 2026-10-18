#nullable enable
namespace TabBlazor;

/// <summary>The outcome of a save or delete handler.</summary>
public enum EditResultStatus
{
    /// <summary>The change was saved (or the item deleted).</summary>
    Succeeded,
    /// <summary>The change failed with a message; the draft is kept.</summary>
    Failed,
    /// <summary>The server rejected some fields; their messages show on the fields and the draft is kept.</summary>
    Invalid
}

/// <summary>A field error returned by a server, for example <c>("customer.name", "Required")</c>.</summary>
public sealed record FieldError(string Field, string Message);

/// <summary>
/// The result of a save or delete handler. Use the factories: <c>EditResult.Saved(item)</c>,
/// <c>EditResult.Deleted()</c>, <c>EditResult.Failed("Order is locked")</c> or
/// <c>EditResult.Invalid(("customer.name", "Required"))</c>. An <see cref="EditResult"/> converts implicitly to
/// <see cref="EditResult{TItem}"/>, so a save handler can return <c>EditResult.Failed(...)</c>.
/// </summary>
public sealed class EditResult
{
    private EditResult(EditResultStatus status, string? message, IReadOnlyList<FieldError> errors)
    {
        Status = status;
        Message = message;
        Errors = errors;
    }

    public EditResultStatus Status { get; }
    public string? Message { get; }
    public IReadOnlyList<FieldError> Errors { get; }
    public bool Succeeded => Status == EditResultStatus.Succeeded;

    /// <summary>The item was saved; it replaces the original (or is inserted when new).</summary>
    public static EditResult<TItem> Saved<TItem>(TItem item) => new(EditResultStatus.Succeeded, item, null, []);

    /// <summary>The item (or items) were deleted.</summary>
    public static EditResult Deleted() => new(EditResultStatus.Succeeded, null, []);

    /// <summary>The operation failed; the message is shown and the draft kept.</summary>
    public static EditResult Failed(string message) => new(EditResultStatus.Failed, message, []);

    /// <summary>Server-side validation errors by field path (<c>customer.name</c>); shown on the matching fields.</summary>
    public static EditResult Invalid(params (string Field, string Message)[] errors) =>
        new(EditResultStatus.Invalid, null, errors.Select(e => new FieldError(e.Field, e.Message)).ToList());

    /// <summary>Server-side validation errors with an overall message.</summary>
    public static EditResult Invalid(string message, params (string Field, string Message)[] errors) =>
        new(EditResultStatus.Invalid, message, errors.Select(e => new FieldError(e.Field, e.Message)).ToList());
}

/// <summary>The result of a save handler: the saved item, or a failure.</summary>
public sealed class EditResult<TItem>
{
    internal EditResult(EditResultStatus status, TItem? item, string? message, IReadOnlyList<FieldError> errors)
    {
        Status = status;
        Item = item;
        Message = message;
        Errors = errors;
    }

    public EditResultStatus Status { get; }

    /// <summary>The saved item, when <see cref="Status"/> is Succeeded.</summary>
    public TItem? Item { get; }

    public string? Message { get; }
    public IReadOnlyList<FieldError> Errors { get; }
    public bool Succeeded => Status == EditResultStatus.Succeeded;

    public static implicit operator EditResult<TItem>(EditResult result) =>
        result.Succeeded
            ? throw new InvalidOperationException("Use EditResult.Saved(item) to report a successful save.")
            : new EditResult<TItem>(result.Status, default, result.Message, result.Errors);
}

/// <summary>How a DataView edits a row.</summary>
public enum DataEditMode
{
    /// <summary>The row turns into editors (table view only; other views use Popup).</summary>
    Inline,
    /// <summary>A modal built from the columns.</summary>
    Popup,
    /// <summary>An offcanvas panel from the right, for wide forms.</summary>
    Drawer,
    /// <summary>Your own UI: <c>OnCustomEdit</c> gets the edit context and calls its <c>SaveAsync</c>/<c>CancelAsync</c>.</summary>
    Custom
}
