
namespace TabBlazor.Components.Tables;

public class TableFilterService
{
    public Expression<Func<T, bool>> GetFilter<T>(IColumn<T> column, string value)
    {
        var property = column.Property;
        if (column.SearchExpression != null)
        {
            var wrappedExpression = Expression.Invoke(column.SearchExpression, property.Parameters[0],
                Expression.Constant(value));
            return Expression.Lambda<Func<T, bool>>(wrappedExpression, property.Parameters);
        }

        var type = column.Type;
        switch (column.Type)
        {
            case not null when type == typeof(string):
                return StringContainsOrdinalIgnoreCase(property, value);
            case not null when type == typeof(Guid) || Nullable.GetUnderlyingType(type) == typeof(Guid):
                return GuidContainsExpression(property, value);
            case not null when type.BaseType == typeof(Enum):
                return EnumTranslationContainsExpression(property, value);
            default:
                return null;
        }
    }

    private Expression<Func<T, bool>> GuidContainsExpression<T>(Expression<Func<T, object>> expression, string value)
    {
        var method = typeof(TableFilterService)
            .GetMethod(nameof(GuidContainsString))!;

        var enumContains = Expression.Call(method, expression.Body, Expression.Constant(value));
        return Expression.Lambda<Func<T, bool>>(
            enumContains,
            expression.Parameters);
    }

    private Expression<Func<T, bool>> EnumTranslationContainsExpression<T>(Expression<Func<T, object>> expression, string value)
    {
        var method = typeof(TableFilterService)
            .GetMethod(nameof(EnumTranslationContains), [typeof(Enum), typeof(string)])!;

        var enumContains = Expression.Call(method, expression.Body, Expression.Constant(value));
        return Expression.Lambda<Func<T, bool>>(
            enumContains,
            expression.Parameters);
    }

    private Expression<Func<T, bool>> StringContainsOrdinalIgnoreCase<T>(Expression<Func<T, object>> expression,
        string value)
    {
        return CallMethodType(expression, typeof(string), nameof(string.Contains),
            [typeof(string), typeof(StringComparison)],
            [value, StringComparison.OrdinalIgnoreCase]);
    }

    private Expression<Func<T, bool>> CallMethodType<T>(Expression<Func<T, object>> expression, Type type, string method,
        Type[] parameters, object[] values)
    {
        var methodInfo = type.GetMethod(method, parameters)!;

        return Expression.Lambda<Func<T, bool>>(
            Expression.Call(
                expression.Body,
                methodInfo,
                values.OrEmptyIfNull().Select(Expression.Constant)),
            expression.Parameters);
    }

    public static bool EnumTranslationContains(object value, string text)
    {
        if (!(value is Enum enumValue))
        {
            return false;
        }

        return enumValue.ToString().Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    public static bool GuidContainsString(object value, string text)
    {
        if (!(value is Guid guid))
        {
            return false;
        }

        return guid.ToString().Contains(text, StringComparison.OrdinalIgnoreCase);
    }
}