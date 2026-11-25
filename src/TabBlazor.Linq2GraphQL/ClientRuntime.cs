using System.Collections;
using System.Reflection;

namespace TabBlazor.Linq2GraphQL;

/// <summary>
/// Recognizes Linq2GraphQL's runtime types by name and namespace, never by assembly. A client generated with
/// <c>--standalone</c> carries its own copy of the runtime (<c>&lt;namespace&gt;.Runtime.GraphInputBase</c>, …), so the
/// adapter works with that copy and with the <c>Linq2GraphQL.Client</c> package alike, and references neither.
/// Only types in <c>Linq2GraphQL.Client</c> or a <c>….Runtime</c> namespace count, so an unrelated type of the same
/// name does not.
/// </summary>
internal static class ClientRuntime
{
    public const string GraphInputBase = nameof(GraphInputBase);
    public const string GraphQLMemberAttribute = nameof(GraphQLMemberAttribute);
    public const string GraphQueryExecutionException = nameof(GraphQueryExecutionException);
    public const string GraphQueryRequestException = nameof(GraphQueryRequestException);

    // GraphBase<T, TGraph>, the base of GraphQuery<T> and GraphCursorQuery<T>.
    private const string GraphBase = "GraphBase`2";

    /// <summary>The type is or derives from a type named <paramref name="name"/>.</summary>
    public static bool Is(Type? type, string name)
    {
        for (; type != null; type = type.BaseType)
        {
            if (type.Name == name && IsRuntimeType(type))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A generated input type: one deriving from a runtime's <c>GraphInputBase</c>.</summary>
    public static bool IsInput(Type type) => Is(type, GraphInputBase);

    /// <summary>The GraphQL name in a property's <c>[GraphQLMember]</c>, or null when it has none.</summary>
    public static string? GraphQLName(PropertyInfo property)
    {
        foreach (var attribute in property.CustomAttributes)
        {
            if (attribute.AttributeType.Name == GraphQLMemberAttribute && IsRuntimeType(attribute.AttributeType) &&
                attribute.ConstructorArguments is [{ Value: string name }, ..])
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>The <c>T</c> of a query's <c>GraphBase&lt;T, TGraph&gt;</c>, or null when it is not a Linq2GraphQL query.</summary>
    public static Type? QueryResultType(Type? queryType)
    {
        for (; queryType != null; queryType = queryType.BaseType)
        {
            if (queryType.IsGenericType && queryType.GetGenericTypeDefinition().Name == GraphBase && IsRuntimeType(queryType))
            {
                return queryType.GetGenericArguments()[0];
            }
        }

        return null;
    }

    /// <summary>
    /// A type of the <c>Linq2GraphQL.Client</c> package or of a standalone copy of it (<c>&lt;namespace&gt;.Runtime</c>,
    /// with its sub-namespaces such as <c>.Runtime.Common</c>).
    /// </summary>
    public static bool IsRuntimeType(Type type)
    {
        var ns = type.Namespace;
        return ns != null &&
               (ns == "Linq2GraphQL.Client" || ns.StartsWith("Linq2GraphQL.Client.", StringComparison.Ordinal) ||
                ns.EndsWith(".Runtime", StringComparison.Ordinal) || ns.Contains(".Runtime.", StringComparison.Ordinal));
    }

    /// <summary>The <c>extensions</c> of each error a <c>GraphQueryExecutionException</c> carries.</summary>
    public static IEnumerable<IDictionary?> ErrorExtensions(Exception exception)
    {
        if (exception.GetType().GetProperty("Errors")?.GetValue(exception) is not IEnumerable errors)
        {
            yield break;
        }

        foreach (var error in errors)
        {
            yield return error?.GetType().GetProperty("Extensions")?.GetValue(error) as IDictionary;
        }
    }
}
