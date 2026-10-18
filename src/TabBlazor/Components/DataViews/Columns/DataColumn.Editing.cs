#nullable enable
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace TabBlazor;

public partial class DataColumn<TItem, TValue>
{
    private Action<TItem, TValue>? setter;

    /// <summary>The editor in edit mode; the context is the draft. Replaces the default editor.</summary>
    [Parameter] public RenderFragment<TItem>? EditorTemplate { get; set; }

    /// <summary>Shows the value but never edits it.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Where the column shows: in the view, in edit forms, or both (the default).</summary>
    [Parameter] public ColumnVisibility ShowIn { get; set; } = ColumnVisibility.ViewAndEdit;

    /// <summary>Makes the value read-only for some rows.</summary>
    [Parameter] public Func<TItem, bool>? Editable { get; set; }

    /// <summary>Values for a dropdown editor, for example a list of customer ids.</summary>
    [Parameter] public IEnumerable<TValue>? Options { get; set; }

    /// <summary>The text of each option of <see cref="Options"/>. Defaults to the formatted value.</summary>
    [Parameter] public Func<TValue, string>? OptionText { get; set; }

    public bool CanEdit(TItem draft) =>
        EditorTemplate != null ||
        (setter != null && !ReadOnly && HasDefaultEditor && (Editable?.Invoke(draft) ?? true));

    private static bool HasDefaultEditor
    {
        get
        {
            var type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
            return type == typeof(string) || type == typeof(bool) || type.IsEnum || IsNumber(type) || IsDate(type);
        }
    }

    // Called from Razor markup: a region keeps these sequence numbers apart from the caller's.
    public void RenderEditor(RenderTreeBuilder builder, TItem draft)
    {
        builder.OpenRegion(1000);
        RenderEditorCore(builder, draft);
        builder.CloseRegion();
    }

    private void RenderEditorCore(RenderTreeBuilder builder, TItem draft)
    {
        if (EditorTemplate != null)
        {
            builder.AddContent(0, EditorTemplate(draft));
            return;
        }

        if (!CanEdit(draft) || Value == null || setter == null)
        {
            builder.OpenElement(1, "span");
            builder.AddAttribute(2, "class", "form-control-plaintext");
            builder.AddContent(3, GetDisplayText(draft));
            builder.CloseElement();
            return;
        }

        TryGetValue(draft, out var current);
        var valueExpression = BindTo(draft);
        var valueChanged = EventCallback.Factory.Create<TValue>(this, v => setter(draft, v));
        var type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        var nullable = Nullable.GetUnderlyingType(typeof(TValue)) != null || !typeof(TValue).IsValueType;

        Type component;
        string css;
        if (Options != null || type.IsEnum || (type == typeof(bool) && nullable))
        {
            component = typeof(InputSelect<TValue>);
            css = "form-select";
        }
        else if (type == typeof(string))
        {
            component = typeof(InputText);
            css = "form-control";
        }
        else if (type == typeof(bool))
        {
            component = typeof(InputCheckbox);
            css = "form-check-input";
        }
        else if (IsNumber(type))
        {
            component = typeof(InputNumber<TValue>);
            css = "form-control";
        }
        else
        {
            component = typeof(InputDate<TValue>);
            css = "form-control";
        }

        builder.OpenComponent(10, component);
        builder.AddComponentParameter(11, "Value", current);
        builder.AddComponentParameter(12, "ValueChanged", valueChanged);
        builder.AddComponentParameter(13, "ValueExpression", valueExpression);
        builder.AddAttribute(14, "class", css);
        builder.AddAttribute(15, "aria-label", ((IDataViewColumn<TItem>)this).Title);
        if (type == typeof(TimeOnly))
        {
            builder.AddComponentParameter(16, "Type", InputDateType.Time);
        }

        if (component == typeof(InputSelect<TValue>))
        {
            builder.AddComponentParameter(17, "ChildContent", (RenderFragment)(b => RenderOptions(b, nullable)));
        }

        builder.CloseComponent();
    }

    public void RenderValidationMessage(RenderTreeBuilder builder, TItem draft)
    {
        if (EditorTemplate != null || setter == null || Value == null)
        {
            return;
        }

        builder.OpenRegion(1001);
        builder.OpenComponent<TabValidationMessage<TValue>>(0);
        builder.AddComponentParameter(1, "For", BindTo(draft));
        builder.AddComponentParameter(2, "CssClass", "invalid-feedback d-block");
        builder.CloseComponent();
        builder.CloseRegion();
    }

    private void RenderOptions(RenderTreeBuilder builder, bool nullable)
    {
        var type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        if (nullable)
        {
            builder.OpenElement(0, "option");
            builder.AddAttribute(1, "value", string.Empty);
            builder.CloseElement();
        }

        IEnumerable<object?> values = Options?.Cast<object?>()
                                       ?? (type.IsEnum ? Enum.GetValues(type).Cast<object?>()
                                           : type == typeof(bool) ? [true, false] : []);
        foreach (var value in values)
        {
            builder.OpenElement(2, "option");
            builder.AddAttribute(3, "value", FormatOptionValue(value));
            builder.AddContent(4, OptionLabel(value));
            builder.CloseElement();
        }
    }

    private string OptionLabel(object? value)
    {
        if (value is TValue typed && OptionText != null)
        {
            return OptionText(typed);
        }

        return value switch
        {
            null => string.Empty,
            Enum e => EnumDisplay.GetName(e),
            IFormattable f => f.ToString(Format, CultureInfo.CurrentCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string FormatOptionValue(object? value) => value switch
    {
        null => string.Empty,
        Enum e => e.ToString(),
        bool b => b ? "true" : "false",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    // () => draft.Customer.Name, so validation and modified state attach to the right object and field.
    private Expression<Func<TValue>> BindTo(TItem draft)
    {
        var body = new ParameterReplacer(Value!.Parameters[0], Expression.Constant(draft, typeof(TItem))).Visit(Value.Body);
        return Expression.Lambda<Func<TValue>>(body);
    }

    private static Action<TItem, TValue>? CompileSetter(Expression<Func<TItem, TValue>>? value)
    {
        if (value == null || DataColumnInfoBuilder.GetMemberPath(value) == null ||
            value.Body is not MemberExpression member)
        {
            return null;
        }

        var writable = member.Member switch
        {
            PropertyInfo property => property.SetMethod is { IsPublic: true },
            FieldInfo field => !field.IsInitOnly && field.IsPublic,
            _ => false
        };
        if (!writable)
        {
            return null;
        }

        var input = Expression.Parameter(typeof(TValue), "value");
        var assign = Expression.Assign(member, input);
        return Expression.Lambda<Action<TItem, TValue>>(assign, value.Parameters[0], input).Compile();
    }

    private static bool IsNumber(Type type) => Type.GetTypeCode(type) is TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or
        TypeCode.Single or TypeCode.Double or TypeCode.Decimal;

    private static bool IsDate(Type type) => type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                                             type == typeof(DateOnly) || type == typeof(TimeOnly);

    private sealed class ParameterReplacer(ParameterExpression parameter, Expression replacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == parameter ? replacement : node;
    }
}
