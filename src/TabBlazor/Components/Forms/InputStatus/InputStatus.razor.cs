using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace TabBlazor;

/// <summary>
/// Wraps an input and shows its save progress: a spinner while saving, then a check with a small "Saved"
/// bubble that fades after <see cref="SavedDuration"/>, or an alert on error. Works with any input: set
/// <see cref="Value"/> to the bound value and <see cref="Save"/> to save it, and every change is saved after
/// <see cref="SaveDelay"/>. Or drive <see cref="State"/> yourself.
/// </summary>
/// <remarks>
/// The status icon sits inside a wrapped <c>input</c> or <c>textarea</c>, and after any other control
/// (selects, pickers, <see cref="NumberInput{TValue}"/>, checkboxes).
/// When the same component can show different records (rows that are re-sorted or filtered, a master-detail
/// form), give it a <c>@key</c> per record: a pending save is then flushed when the record's component goes away
/// instead of being mistaken for an edit of the next record.
/// </remarks>
public partial class InputStatus
{
    /// <summary>
    /// The bound value of the wrapped input. Every change, from any source, is saved by <see cref="Save"/>;
    /// changing it back to the last saved value before the save starts cancels the save. Pass the edited
    /// value itself, not the object that holds it: changes inside the same instance are not detected.
    /// </summary>
    [Parameter] public object Value { get; set; }
    /// <summary>
    /// Saves the current <see cref="Value"/>. Throwing shows <see cref="InputSaveState.Error"/> with
    /// <see cref="ErrorText"/>; the save is retried on the next change or when the error icon is clicked.
    /// </summary>
    [Parameter] public Func<Task> Save { get; set; }
    /// <summary>
    /// How long <see cref="Value"/> must stay unchanged before it is saved, so a burst of changes (typing,
    /// stepper clicks) becomes one save. Defaults to 500 milliseconds.
    /// </summary>
    [Parameter] public TimeSpan SaveDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    /// <summary>
    /// The field saved by <see cref="Save"/>. Inside a form, saving waits until this field is valid and marks it
    /// unmodified once saved. Without it, saving waits until the whole form is valid.
    /// </summary>
    [Parameter] public Expression<Func<object>> For { get; set; }
    /// <summary>Raised with the exception thrown by <see cref="Save"/>.</summary>
    [Parameter] public EventCallback<Exception> SaveFailed { get; set; }
    /// <summary>
    /// The shown state. Set it to drive the indicator yourself instead of using <see cref="Save"/>; bind it with
    /// <c>@bind-State</c> to follow the component's own changes, such as fading back to
    /// <see cref="InputSaveState.Idle"/> after <see cref="SavedDuration"/>.
    /// </summary>
    [Parameter] public InputSaveState State { get; set; }
    /// <summary>Raised when the component changes <see cref="State"/> itself.</summary>
    [Parameter] public EventCallback<InputSaveState> StateChanged { get; set; }
    /// <summary>Text shown in the bubble after a successful save. Defaults to "Saved".</summary>
    [Parameter] public string SavedText { get; set; } = "Saved";
    /// <summary>Text announced to screen readers while saving. Defaults to "Saving".</summary>
    [Parameter] public string SavingText { get; set; } = "Saving";
    /// <summary>Text shown in the bubble when the save failed. Defaults to "Could not save".</summary>
    [Parameter] public string ErrorText { get; set; } = "Could not save";
    /// <summary>Tooltip and accessible label of the error icon, which retries the save. Defaults to "Retry".</summary>
    [Parameter] public string RetryText { get; set; } = "Retry";
    /// <summary>How long the saved check and bubble stay visible. Defaults to 2 seconds.</summary>
    [Parameter] public TimeSpan SavedDuration { get; set; } = TimeSpan.FromSeconds(2);
    /// <summary>Raised when the saved indicator has faded.</summary>
    [Parameter] public EventCallback SavedHidden { get; set; }

    [CascadingParameter] private EditContext EditContext { get; set; }
    [Inject] private IServiceProvider Services { get; set; }

    private InputSaveState currentState;
    private InputSaveState appliedStateParameter;
    private bool isTrackingValue;
    private object observedValue;
    private object savedValue;
    private bool isSaving;
    private bool isBlockedByValidation;
    private bool disposed;
    private EditContext subscribedEditContext;
    private CancellationTokenSource pendingSave;
    private CancellationTokenSource hideSaved;

    private string AnnouncedText => currentState switch
    {
        InputSaveState.Saving => SavingText,
        InputSaveState.Saved => SavedText,
        InputSaveState.Error => ErrorText,
        _ => null
    };

    private string SavedDurationStyle =>
        $"--input-status-duration: {SavedDuration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}ms";

    protected override void OnParametersSet()
    {
        if (State != appliedStateParameter)
        {
            appliedStateParameter = State;
            SetState(State);
        }

        SubscribeToValidation();

        if (Save == null) return;

        if (!isTrackingValue)
        {
            isTrackingValue = true;
            observedValue = savedValue = Value;
            return;
        }

        if (!Equals(Value, observedValue))
        {
            observedValue = Value;
            ScheduleSave();
        }
    }

    private void SubscribeToValidation()
    {
        if (ReferenceEquals(EditContext, subscribedEditContext)) return;

        UnsubscribeFromValidation();
        subscribedEditContext = EditContext;
        subscribedEditContext?.OnValidationStateChanged += SaveWhenNoLongerBlocked;
    }

    private void UnsubscribeFromValidation()
    {
        subscribedEditContext?.OnValidationStateChanged -= SaveWhenNoLongerBlocked;
        subscribedEditContext = null;
    }

    private void SaveWhenNoLongerBlocked(object sender, ValidationStateChangedEventArgs args)
    {
        if (isBlockedByValidation && !HasValidationErrors())
        {
            isBlockedByValidation = false;
            ScheduleSave();
        }
    }

    private void ScheduleSave()
    {
        Cancel(ref pendingSave);
        pendingSave = new CancellationTokenSource();
        _ = RunAfterDelayAsync(SaveDelay, SaveNowAsync, pendingSave.Token);
    }

    private async Task RunAfterDelayAsync(TimeSpan delay, Func<Task> action, CancellationToken token)
    {
        var timeProvider = Services.GetService<TimeProvider>() ?? TimeProvider.System;
        await Task.Delay(delay, timeProvider, token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        await InvokeAsync(async () =>
        {
            if (token.IsCancellationRequested) return;

            try
            {
                await action();
            }
            catch (Exception ex)
            {
                await DispatchExceptionAsync(ex);
            }
        });
    }

    private Task SaveNowAsync()
    {
        Cancel(ref pendingSave);
        return SaveIfChangedAsync();
    }

    private async Task SaveIfChangedAsync()
    {
        if (disposed || isSaving) return;

        if (Equals(Value, savedValue))
        {
            if (currentState == InputSaveState.Error)
            {
                await ChangeStateAsync(InputSaveState.Idle);
                StateHasChanged();
            }
            return;
        }

        isBlockedByValidation = HasValidationErrors();
        if (isBlockedByValidation) return;

        var value = Value;
        isSaving = true;
        await ChangeStateAsync(InputSaveState.Saving);
        StateHasChanged();

        Exception failure = null;
        try
        {
            await Save();
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        isSaving = false;
        if (disposed) return;

        if (failure == null)
        {
            savedValue = value;
            if (For != null)
            {
                EditContext?.MarkAsUnmodified(FieldIdentifier.Create(For));
            }
            await ChangeStateAsync(InputSaveState.Saved);
        }
        else
        {
            await ChangeStateAsync(InputSaveState.Error);
            await SaveFailed.InvokeAsync(failure);
        }

        StateHasChanged();
        if (!Equals(Value, value))
        {
            ScheduleSave();
        }
    }

    private bool HasValidationErrors()
    {
        if (EditContext == null) return false;

        return For == null
            ? EditContext.GetValidationMessages().Any()
            : EditContext.GetValidationMessages(FieldIdentifier.Create(For)).Any();
    }

    private Task ChangeStateAsync(InputSaveState state)
    {
        SetState(state);
        if (!StateChanged.HasDelegate) return Task.CompletedTask;

        appliedStateParameter = state;
        return StateChanged.InvokeAsync(state);
    }

    private void SetState(InputSaveState state)
    {
        Cancel(ref hideSaved);
        currentState = state;
        if (state == InputSaveState.Saved)
        {
            hideSaved = new CancellationTokenSource();
            _ = RunAfterDelayAsync(SavedDuration, HideSavedAsync, hideSaved.Token);
        }
    }

    private async Task HideSavedAsync()
    {
        await ChangeStateAsync(InputSaveState.Idle);
        await SavedHidden.InvokeAsync();
        StateHasChanged();
    }

    private void FlushPendingSave()
    {
        if (pendingSave == null || isSaving || Save == null || Equals(Value, savedValue) || HasValidationErrors()) return;

        _ = SaveDetachedAsync(Save);
    }

    private async Task SaveDetachedAsync(Func<Task> save)
    {
        try
        {
            await save();
        }
        catch (Exception ex)
        {
            await SaveFailed.InvokeAsync(ex);
        }
    }

    private static void Cancel(ref CancellationTokenSource source)
    {
        source?.Cancel();
        source?.Dispose();
        source = null;
    }

    protected override string ClassNames => ClassBuilder
        .Add("input-status")
        .AddIf("input-status-active", currentState != InputSaveState.Idle)
        .AddIf("input-status-busy", currentState == InputSaveState.Saving)
        .AddIf("input-status-failed", currentState == InputSaveState.Error)
        .ToString();

    public void Dispose()
    {
        FlushPendingSave();
        disposed = true;
        Cancel(ref pendingSave);
        Cancel(ref hideSaved);
        UnsubscribeFromValidation();
    }
}
