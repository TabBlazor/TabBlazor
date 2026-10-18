#nullable enable
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace TabBlazor;

/// <summary>
/// One edit in progress: the original row and the draft being edited. The original is never changed; the draft
/// replaces it only when the save handler returns <c>EditResult.Saved</c>.
/// </summary>
public sealed class DataEditContext<TItem>
{
    private readonly DataView<TItem> view;

    internal DataEditContext(DataView<TItem> view, TItem? original, TItem draft, bool isNew)
    {
        this.view = view;
        Original = original;
        Draft = draft;
        IsNew = isNew;
        EditContext = new EditContext(draft!);
        MessageStore = new ValidationMessageStore(EditContext);
        EditContext.OnFieldChanged += (_, args) => MessageStore.Clear(args.FieldIdentifier);
    }

    /// <summary>The row as it was loaded; null when adding.</summary>
    public TItem? Original { get; }

    /// <summary>The copy being edited.</summary>
    public TItem Draft { get; }

    /// <summary>True when adding a new item.</summary>
    public bool IsNew { get; }

    /// <summary>The Blazor edit context of the draft (validation, modified state).</summary>
    public EditContext EditContext { get; }

    /// <summary>True while the save handler runs.</summary>
    public bool IsSaving { get; internal set; }

    /// <summary>The message of a failed save, shown on the row or form.</summary>
    public string? Message { get; internal set; }

    /// <summary>True when any field of the draft was changed.</summary>
    public bool IsModified => EditContext.IsModified();

    internal ValidationMessageStore MessageStore { get; }

    internal object? ValidatorInstance { get; set; }

    /// <summary>Validates the draft and runs the save handler (for custom edit UIs).</summary>
    public Task<bool> SaveAsync() => view.SaveEditAsync(this);

    /// <summary>Throws the draft away.</summary>
    public Task CancelAsync() => view.CancelEditAsync(force: true);

    /// <summary>Shows server-side errors on the matching fields. Paths are member paths such as <c>customer.name</c>.</summary>
    internal List<string> ApplyFieldErrors(IEnumerable<FieldError> errors)
    {
        var unmatched = new List<string>();
        foreach (var error in errors)
        {
            var field = ResolveField(Draft, error.Field);
            if (field is { } identifier)
            {
                MessageStore.Add(identifier, error.Message);
            }
            else
            {
                unmatched.Add(error.Message);
            }
        }

        EditContext.NotifyValidationStateChanged();
        return unmatched;
    }

    // Walks a camelCase member path to the object that owns the last member.
    internal static FieldIdentifier? ResolveField(object? model, string path)
    {
        if (model == null || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var segments = path.Split('.');
        var owner = model;
        for (var i = 0; i < segments.Length; i++)
        {
            var property = owner.GetType().GetProperty(segments[i],
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null)
            {
                return null;
            }

            if (i == segments.Length - 1)
            {
                return new FieldIdentifier(owner, property.Name);
            }

            owner = property.GetValue(owner);
            if (owner == null)
            {
                return null;
            }
        }

        return null;
    }
}

/// <summary>Passed to <c>OnEditStarting</c>; set <see cref="Cancel"/> to prevent the edit.</summary>
public sealed class DataEditStartingEventArgs<TItem>(TItem? item, bool isNew)
{
    /// <summary>The row to edit; null when adding.</summary>
    public TItem? Item { get; } = item;

    public bool IsNew { get; } = isNew;

    public bool Cancel { get; set; }
}
