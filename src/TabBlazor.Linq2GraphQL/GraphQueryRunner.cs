using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace TabBlazor.Linq2GraphQL;

/// <summary>One page read from a generated connection (cursor) or collection segment (offset) type.</summary>
internal sealed record GraphQLPageResult<TNode>(IReadOnlyList<TNode> Items, int? TotalCount, bool HasNextPage, string? EndCursor);

/// <summary>
/// Runs the query a page function returns (<c>GraphCursorQuery&lt;TConnection&gt;</c> or <c>GraphQuery&lt;TSegment&gt;</c>)
/// and reads nodes, total count and page info through their <c>[GraphQLMember]</c> names, so users need no selectors.
/// </summary>
internal static class GraphQueryRunner
{
    private static readonly ConcurrentDictionary<Type, QueryShape> Shapes = new();

    /// <param name="query">The query the page function returned.</param>
    /// <param name="nodeFields">The row fields to fetch, or null for all of the rows' scalar fields.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public static async Task<GraphQLPageResult<TNode>> ExecuteAsync<TNode>(object query, IReadOnlyList<PropertyInfo[]>? nodeFields,
        CancellationToken cancellationToken)
    {
        var shape = Shapes.GetOrAdd(query.GetType(), QueryShape.For);
        shape.IncludeResultFields(query, nodeFields);

        var execute = shape.Select.Invoke(query, null)!;
        var task = (Task)shape.ExecuteAsync(execute.GetType()).Invoke(execute, [cancellationToken])!;
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex) when (GraphQLCostLimitException.From(ex) is { } costError)
        {
            throw costError;
        }

        var result = task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task);
        if (result == null)
        {
            return new GraphQLPageResult<TNode>([], 0, false, null);
        }

        var items = shape.Items.GetValue(result) is IEnumerable list ? list.Cast<TNode>().ToList() : [];
        int? total = shape.TotalCount?.GetValue(result) is int count ? count : null;
        var pageInfo = shape.PageInfo?.GetValue(result);
        var hasNext = pageInfo?.GetType().GetProperty(nameof(PageInfo.HasNextPage))?.GetValue(pageInfo) is true;
        var endCursor = pageInfo?.GetType().GetProperty(nameof(PageInfo.EndCursor))?.GetValue(pageInfo) as string;
        return new GraphQLPageResult<TNode>(items, total, hasNext, endCursor);
    }

    private sealed class QueryShape
    {
        public required MethodInfo Select { get; init; }
        public required PropertyInfo Items { get; init; }
        public PropertyInfo? TotalCount { get; init; }
        public PropertyInfo? PageInfo { get; init; }
        public required Type ResultType { get; init; }
        public required MethodInfo Include { get; init; }

        private readonly ConcurrentDictionary<Type, MethodInfo> executeMethods = new();

        public static QueryShape For(Type queryType)
        {
            var resultType = FindResultType(queryType)
                             ?? throw new InvalidOperationException(
                                 $"The page function must return a Linq2GraphQL query (GraphCursorQuery<T> or GraphQuery<T>), not {queryType.Name}.");
            var members = GraphQLMembers.Of(resultType);
            var items = members.GetValueOrDefault("nodes") ?? members.GetValueOrDefault("items")
                        ?? throw new InvalidOperationException(
                            $"{resultType.Name} has neither 'nodes' nor 'items'; the query must return a paged connection or collection segment.");

            return new QueryShape
            {
                ResultType = resultType,
                Select = queryType.GetMethod("Select", Type.EmptyTypes)
                         ?? throw new InvalidOperationException($"{queryType.Name} has no Select() method."),
                Include = queryType.GetMethods().Single(m => m.Name == "Include" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1),
                Items = items,
                TotalCount = members.GetValueOrDefault("totalCount"),
                PageInfo = members.GetValueOrDefault("pageInfo")
            };
        }

        private static Type? FindResultType(Type? type)
        {
            for (; type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(GraphBase<,>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return null;
        }

        public MethodInfo ExecuteAsync(Type executeType) => executeMethods.GetOrAdd(executeType, static t =>
            t.GetMethod("ExecuteAsync", [typeof(CancellationToken)])
            ?? throw new InvalidOperationException($"{t.Name} has no ExecuteAsync(CancellationToken)."));

        // The page needs its rows (their scalar fields, or the given fields), the total and the page info, whatever
        // the page function included. Including the rows as such makes Linq2GraphQL add all their scalar fields;
        // including paths below them adds just those.
        public void IncludeResultFields(object query, IReadOnlyList<PropertyInfo[]>? nodeFields)
        {
            if (nodeFields is { Count: > 0 })
            {
                foreach (var fields in nodeFields)
                {
                    IncludeNodeField(query, fields);
                }
            }
            else
            {
                IncludeMember(query, Items);
            }

            if (TotalCount != null)
            {
                IncludeMember(query, TotalCount);
            }

            if (PageInfo != null)
            {
                IncludeMember(query, PageInfo);
            }
        }

        // c => c.Nodes.Select(n => n.Customer.Name)
        private void IncludeNodeField(object query, PropertyInfo[] fields)
        {
            var nodeType = GraphQLMembers.ElementType(Items.PropertyType);
            var node = Expression.Parameter(nodeType, "n");
            Expression body = node;
            foreach (var field in fields)
            {
                body = Expression.Property(body, field);
            }

            var connection = Expression.Parameter(ResultType, "c");
            var select = Expression.Call(typeof(Enumerable), nameof(Enumerable.Select), [nodeType, body.Type],
                Expression.Property(connection, Items), Expression.Lambda(body, node));
            Include.MakeGenericMethod(select.Type).Invoke(query, [Expression.Lambda(select, connection)]);
        }

        private void IncludeMember(object query, PropertyInfo member)
        {
            var parameter = Expression.Parameter(ResultType, "c");
            var lambda = Expression.Lambda(Expression.Property(parameter, member), parameter);
            Include.MakeGenericMethod(member.PropertyType).Invoke(query, [lambda]);
        }
    }
}
