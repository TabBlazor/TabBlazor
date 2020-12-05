//using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
//using Swegon.Components;

namespace TabBlazor.Components.Tables
{
    public class TableFilterService
    {
        // private readonly IStringLocalizer<object> loc;

        //public TableFilterService(IStringLocalizer<object> loc)
        //{
        //    this.loc = loc;
        //}

        public Expression<Func<T, bool>> GetFilter<T>(IColumn<T> column, string value)
        {
            var property = column.Property;
            if (column.SearchExpression != null)
            {
                var wrappedExpression = Expression.Invoke(column.SearchExpression, property.Parameters[0], Expression.Constant(value));
                return Expression.Lambda<Func<T, bool>>(wrappedExpression, property.Parameters);
            }

            var type = column.Type;
            switch (type)
            {
                case Type _ when type == typeof(string):
                    return StringContainsOrdinalIgnoreCase(property, value);
                case Type _ when type.BaseType == typeof(Enum):
                    return EnumTranslationContains(property, value);
                default:
                    return null;
            }
        }

        public Expression<Func<T, bool>> EnumTranslationContains<T>(Expression<Func<T, object>> expression, string value)
        {
            var method = typeof(TableFilterService)
                    .GetMethod(nameof(TableFilterService.EnumTranslationContains), new[] { typeof(Enum), typeof(string) });

            // var enumContains = Expression.Call(method, expression.Body, Expression.Constant(value), Expression.Constant(loc));
            var enumContains = Expression.Call(method, expression.Body, Expression.Constant(value));
            return Expression.Lambda<Func<T, bool>>(
                enumContains,
                expression.Parameters);
        }

        public Expression<Func<T, bool>> StringContainsOrdinalIgnoreCase<T>(Expression<Func<T, object>> expression, string value)
        {
            return CallMethodType(expression, typeof(string), nameof(string.Contains), new[] { typeof(string), typeof(StringComparison) }, new object[] { value, StringComparison.OrdinalIgnoreCase });
        }

        public Expression<Func<T, bool>> CallMethodType<T>(Expression<Func<T, object>> expression, Type type, string method, Type[] parameters, object[] values)
        {
            MethodInfo methodInfo = type.GetMethod(method, parameters);

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

        //public static bool EnumTranslationContains(object value, string text, IStringLocalizer<object> loc)
        //{
        //    if (!(value is Enum enumValue))
        //    {
        //        return false;
        //    }
        //    return enumValue.ToString().Contains(text, StringComparison.OrdinalIgnoreCase);
        //    // return loc.GetString(enumValue).ToString().Contains(text, StringComparison.OrdinalIgnoreCase);
        //}
    }
}
