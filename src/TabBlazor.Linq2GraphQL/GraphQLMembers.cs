using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Linq2GraphQL.Client;

namespace TabBlazor.Linq2GraphQL;

/// <summary>
/// Reads the shape of Linq2GraphQL's generated code: public properties carrying <see cref="GraphQLMemberAttribute"/>.
/// Lookups are made once per type and cached. Class names are never inspected.
/// </summary>
internal static class GraphQLMembers
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>> ByGraphQLName = new();

    /// <summary>The GraphQL members of a type, by GraphQL name.</summary>
    public static IReadOnlyDictionary<string, PropertyInfo> Of(Type type) => ByGraphQLName.GetOrAdd(type, static t =>
        t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => (Property: p, Attribute: p.GetCustomAttribute<GraphQLMemberAttribute>()))
            .Where(p => p.Attribute != null)
            .GroupBy(p => p.Attribute!.GraphQLName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Property, StringComparer.Ordinal));

    public static PropertyInfo? Find(Type type, string graphQLName) => Of(type).GetValueOrDefault(graphQLName);

    /// <summary>The element type of a list, the underlying type of a nullable, or the type itself.</summary>
    public static Type ElementType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type != typeof(string) && type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return type.GetGenericArguments()[0];
        }

        return type;
    }

    public static bool IsInput(Type type) => typeof(GraphInputBase).IsAssignableFrom(ElementType(type));

    /// <summary>
    /// A leaf (operation) input is recognized by structure: apart from <c>and</c>/<c>or</c>, its members are scalars or
    /// lists of scalars, for example <c>StringOperationFilterInput</c> with <c>eq</c>, <c>contains</c>, <c>in</c>.
    /// </summary>
    public static bool IsLeafInput(Type type, string and, string or) =>
        typeof(GraphInputBase).IsAssignableFrom(type) &&
        Of(type).Where(m => m.Key != and && m.Key != or).All(m => !IsInput(m.Value.PropertyType)) &&
        Of(type).Count > 2;

    /// <summary>
    /// Maps a DataView member path (camelCase C# names, <c>customer.name</c>) on a generated row type to its GraphQL
    /// path through the <see cref="GraphQLMemberAttribute"/> of each member; null when a member is not found.
    /// </summary>
    public static string? ToGraphQLPath(Type rowType, string memberPath)
    {
        var segments = new List<string>();
        var type = rowType;
        foreach (var segment in memberPath.Split('.'))
        {
            var property = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => string.Equals(JsonNamingPolicy.CamelCase.ConvertName(p.Name), segment, StringComparison.OrdinalIgnoreCase));
            var attribute = property?.GetCustomAttribute<GraphQLMemberAttribute>();
            if (property == null || attribute == null)
            {
                return null;
            }

            segments.Add(attribute.GraphQLName);
            type = ElementType(property.PropertyType);
        }

        return string.Join(".", segments);
    }
}
