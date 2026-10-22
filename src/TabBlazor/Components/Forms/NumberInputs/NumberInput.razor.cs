using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace TabBlazor;

/// <summary>
/// A number field between a decrease and an increase button, bound to a numeric <typeparamref name="TValue"/>
/// such as <see cref="int"/>, <see cref="decimal"/> or <see cref="double"/> (or a nullable variant).
/// Keeps the value between <see cref="Min"/> and <see cref="Max"/>, rounds it to the precision of
/// <see cref="Step"/>, keeps stepping while a button is held and steps with the arrow keys.
/// </summary>
public partial class NumberInput<TValue> : TablerBaseComponent, IDisposable
{
    private const int Decrement = -1;
    private const int Increment = 1;

    private static readonly Type ValueType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
    private static readonly bool IsNullable = ValueType != typeof(TValue);
    private static readonly bool IsIntegral = Type.GetTypeCode(ValueType) is not (TypeCode.Single or TypeCode.Double or TypeCode.Decimal);

    private readonly string generatedId = $"number-input-{Guid.CreateVersion7():N}";
    private FieldIdentifier? fieldIdentifier;
    private string fieldCssClasses;
    private decimal? currentValue;
    private string text = "";
    private decimal? textValue;
    private string lastText = "";
    private decimal typeMin;
    private decimal typeMax;
    private decimal min;
    private decimal max;
    private decimal step;
    private CancellationTokenSource repeatCancellation;
    private int? heldDirection;

    /// <summary>The bound value. Supports two-way binding via <c>@bind-Value</c>.</summary>
    [Parameter] public TValue Value { get; set; }
    /// <summary>Raised when the value changes, from typing, a button or the keyboard.</summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }
    /// <summary>Identifies the bound field for validation; set automatically by <c>@bind-Value</c>.</summary>
    [Parameter] public Expression<Func<TValue>> ValueExpression { get; set; }
    /// <summary>The label text shown above the input.</summary>
    [Parameter] public string Label { get; set; }
    /// <summary>Lower bound. When null, only the range of <typeparamref name="TValue"/> applies.</summary>
    [Parameter] public double? Min { get; set; }
    /// <summary>Upper bound. When null, only the range of <typeparamref name="TValue"/> applies.</summary>
    [Parameter] public double? Max { get; set; }
    /// <summary>
    /// Amount added or removed per step. Its decimals set the precision of the value, so <c>0.25</c>
    /// always shows two decimals. Defaults to 1.
    /// </summary>
    [Parameter] public double Step { get; set; } = 1;
    /// <summary>The size of the field and its buttons. Defaults to <see cref="NumberInputSize.Default"/>.</summary>
    [Parameter] public NumberInputSize Size { get; set; } = NumberInputSize.Default;
    /// <summary>Optional CSS length for the field between the buttons, e.g. <c>"6rem"</c>. Defaults to 3.5rem, enough for a quantity.</summary>
    [Parameter] public string InputWidth { get; set; }
    /// <summary>Disables the field and both buttons.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Makes the field read-only and disables both buttons.</summary>
    [Parameter] public bool ReadOnly { get; set; }
    /// <summary>When true, keeps stepping while a button is held down. Defaults to true.</summary>
    [Parameter] public bool Repeat { get; set; } = true;
    /// <summary>Milliseconds a button has to be held before it starts repeating. Defaults to 400.</summary>
    [Parameter] public int RepeatDelay { get; set; } = 400;
    /// <summary>Milliseconds between two repeated steps. Defaults to 80.</summary>
    [Parameter] public int RepeatInterval { get; set; } = 80;
    /// <summary>Accessible name of the decrease button. Defaults to "Decrease".</summary>
    [Parameter] public string DecrementLabel { get; set; } = "Decrease";
    /// <summary>Accessible name of the increase button. Defaults to "Increase".</summary>
    [Parameter] public string IncrementLabel { get; set; } = "Increase";

    [CascadingParameter] private EditContext CascadedEditContext { get; set; }

    protected string InputId => UnmatchedParameters?.TryGetValue("id", out var id) == true ? id?.ToString() : generatedId;
    protected string InputMode => Precision > 0 ? "decimal" : "numeric";
    protected string AriaValueMin => Min.HasValue ? min.ToString(CultureInfo.InvariantCulture) : null;
    protected string AriaValueMax => Max.HasValue ? max.ToString(CultureInfo.InvariantCulture) : null;
    protected string AriaValueNow => textValue?.ToString(CultureInfo.InvariantCulture);
    protected string Style => AppendCssVariable("--tblr-number-input-input-width", InputWidth);

    protected string InputClassNames => new ClassBuilder("form-control")
        .Add(fieldCssClasses)
        .ToString();

    protected override string ClassNames => ClassBuilder
        .Add("number-input")
        .AddCompare("number-input-sm", Size, NumberInputSize.Small)
        .AddCompare("number-input-lg", Size, NumberInputSize.Large)
        .ToString();

    private int Precision => IsIntegral ? 0 : step.Scale;
    private bool IsLocked => Disabled || ReadOnly;

    private string Text
    {
        get => text;
        set
        {
            text = value;
            textValue = Parse(value);
        }
    }

    protected override void OnInitialized()
    {
        (typeMin, typeMax) = GetTypeRange();

        if (ValueExpression != null)
        {
            fieldIdentifier = FieldIdentifier.Create(ValueExpression);
        }

        if (CascadedEditContext != null)
        {
            CascadedEditContext.OnValidationStateChanged += SetValidationClasses;
        }
    }

    protected override void OnParametersSet()
    {
        step = Step > 0 && double.IsFinite(Step) ? ClampToDecimal(Step) : 1;
        min = Min.HasValue ? Math.Max(ClampToDecimal(Min.Value), typeMin) : typeMin;
        max = Max.HasValue ? Math.Min(ClampToDecimal(Max.Value), typeMax) : typeMax;

        var incoming = ToDecimal(Value);
        if (incoming != currentValue)
        {
            currentValue = incoming;
            Text = Display(incoming);
            lastText = Text;
        }

        base.OnParametersSet();
    }

    private void OnInput(ChangeEventArgs args)
    {
        Text = args.Value?.ToString();
    }

    private async Task OnChangeAsync(ChangeEventArgs args)
    {
        Text = args.Value?.ToString();
        if (Text == lastText)
        {
            return;
        }

        if (textValue.HasValue)
        {
            await CommitAsync(textValue);
        }
        else if (IsNullable && string.IsNullOrWhiteSpace(Text))
        {
            await CommitAsync(null);
        }
        else
        {
            Text = lastText;
        }
    }

    private Task OnKeyDownAsync(KeyboardEventArgs args) => args.Key switch
    {
        "ArrowUp" => StepByAsync(Increment),
        "ArrowDown" => StepByAsync(Decrement),
        "Home" when Min.HasValue && !IsLocked => CommitAsync(min),
        "End" when Max.HasValue && !IsLocked => CommitAsync(max),
        _ => Task.CompletedTask
    };

    private async Task OnButtonClickAsync(MouseEventArgs args, int direction)
    {
        var isPointerClick = args.Detail > 0;
        var endsHold = isPointerClick && heldDirection == direction;
        heldDirection = null;
        if (endsHold)
        {
            return;
        }

        await StepByAsync(direction);
    }

    private void StartRepeat(int direction)
    {
        StopRepeat();
        heldDirection = null;

        if (!Repeat || IsStepDisabled(direction))
        {
            return;
        }

        repeatCancellation = new CancellationTokenSource();
        _ = RepeatAsync(direction, repeatCancellation.Token);
    }

    private async Task RepeatAsync(int direction, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(RepeatDelay, cancellationToken);

            while (!IsStepDisabled(direction))
            {
                await InvokeAsync(async () =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    heldDirection = direction;
                    await StepByAsync(direction);
                    StateHasChanged();
                });

                await Task.Delay(RepeatInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            await DispatchExceptionAsync(exception);
        }
    }

    private void StopRepeat()
    {
        repeatCancellation?.Cancel();
        repeatCancellation?.Dispose();
        repeatCancellation = null;
    }

    private async Task StepByAsync(int direction)
    {
        if (IsLocked)
        {
            return;
        }

        var current = textValue ?? Normalize(0);
        var next = direction == Increment
            ? current > max - step ? max : current + step
            : current < min + step ? min : current - step;

        await CommitAsync(next);
    }

    private async Task CommitAsync(decimal? value)
    {
        var next = value.HasValue ? Normalize(value.Value) : (decimal?)null;
        Text = Display(next);
        lastText = Text;

        if (next == currentValue)
        {
            return;
        }

        currentValue = next;
        await ValueChanged.InvokeAsync(ToValue(next));

        if (fieldIdentifier is { } fid)
        {
            CascadedEditContext?.NotifyFieldChanged(fid);
        }

        if (next == min || next == max)
        {
            StopRepeat();
        }
    }

    private bool IsStepDisabled(int direction)
    {
        if (IsLocked)
        {
            return true;
        }

        if (textValue is not { } value)
        {
            return false;
        }

        return direction == Increment ? value >= max : value <= min;
    }

    private decimal Normalize(decimal value) =>
        Math.Min(max, Math.Max(min, Math.Round(value, Precision, MidpointRounding.AwayFromZero)));

    private string Display(decimal? value) =>
        value?.ToString($"F{Math.Max(Precision, value.Value.Scale)}", CultureInfo.CurrentCulture) ?? "";

    private static decimal? Parse(string value) =>
        decimal.TryParse(value,
            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.CurrentCulture, out var result)
            ? result
            : null;

    private static decimal? ToDecimal(TValue value) => value switch
    {
        null => null,
        double number => double.IsFinite(number) ? ClampToDecimal(number) : null,
        float number => float.IsFinite(number) ? FloatToDecimal(number) : null,
        _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
    };

    private static TValue ToValue(decimal? value) => value.HasValue
        ? (TValue)Convert.ChangeType(value.Value, ValueType, CultureInfo.InvariantCulture)
        : default;

    private static decimal ClampToDecimal(double value)
    {
        if (value <= (double)decimal.MinValue)
        {
            return decimal.MinValue;
        }

        return value >= (double)decimal.MaxValue ? decimal.MaxValue : (decimal)value;
    }

    private static decimal FloatToDecimal(float value) =>
        Math.Abs(value) < 7.9e28f ? (decimal)value : ClampToDecimal(value);

    private static (decimal Min, decimal Max) GetTypeRange()
    {
        if (ValueType.IsEnum)
        {
            throw UnsupportedType();
        }

        return Type.GetTypeCode(ValueType) switch
        {
            TypeCode.SByte => (sbyte.MinValue, sbyte.MaxValue),
            TypeCode.Byte => (byte.MinValue, byte.MaxValue),
            TypeCode.Int16 => (short.MinValue, short.MaxValue),
            TypeCode.UInt16 => (ushort.MinValue, ushort.MaxValue),
            TypeCode.Int32 => (int.MinValue, int.MaxValue),
            TypeCode.UInt32 => (uint.MinValue, uint.MaxValue),
            TypeCode.Int64 => (long.MinValue, long.MaxValue),
            TypeCode.UInt64 => (ulong.MinValue, ulong.MaxValue),
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal => (decimal.MinValue, decimal.MaxValue),
            _ => throw UnsupportedType()
        };
    }

    private static InvalidOperationException UnsupportedType() =>
        new($"NumberInput does not support {typeof(TValue)}. Use a numeric type such as int, decimal or double.");

    private void SetValidationClasses(object sender, ValidationStateChangedEventArgs args)
    {
        if (fieldIdentifier is not { } fid)
        {
            return;
        }

        fieldCssClasses = CascadedEditContext?.FieldCssClass(fid) ?? "";
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        StopRepeat();

        if (CascadedEditContext != null)
        {
            CascadedEditContext.OnValidationStateChanged -= SetValidationClasses;
        }
    }
}

/// <summary>The size of a <see cref="NumberInput{TValue}"/>.</summary>
public enum NumberInputSize
{
    /// <summary>Matches a default form control.</summary>
    Default,
    /// <summary>Matches a small form control.</summary>
    Small,
    /// <summary>Matches a large form control.</summary>
    Large
}
