namespace TabBlazor.Components.QuickTables;

public class GridSort<TGridItem>
{
    private const string ExpressionNotRepresentableMessage =
        "The supplied expression can't be represented as a property name for sorting. Only simple member expressions, such as @(x => x.SomeProperty), can be converted to property names.";

    private readonly Func<IQueryable<TGridItem>, bool, IOrderedQueryable<TGridItem>> first;

    private readonly (LambdaExpression, bool) firstExpression;

    private IReadOnlyCollection<(string PropertyName, SortDirection Direction)> cachedPropertyListAscending;
    private IReadOnlyCollection<(string PropertyName, SortDirection Direction)> cachedPropertyListDescending;
    private List<Func<IOrderedQueryable<TGridItem>, bool, IOrderedQueryable<TGridItem>>> then;
    private List<(LambdaExpression, bool)> thenExpressions;

    internal GridSort(Func<IQueryable<TGridItem>, bool, IOrderedQueryable<TGridItem>> first,
        (LambdaExpression, bool) firstExpression)
    {
        this.first = first;
        this.firstExpression = firstExpression;
        then = default;
        thenExpressions = default;
    }

    public static GridSort<TGridItem> ByAscending<U>(Expression<Func<TGridItem, U>> expression)
    {
        return new GridSort<TGridItem>(
            (queryable, asc) => asc ? queryable.OrderBy(expression) : queryable.OrderByDescending(expression),
            (expression, true));
    }

    public static GridSort<TGridItem> ByDescending<U>(Expression<Func<TGridItem, U>> expression)
    {
        return new GridSort<TGridItem>(
            (queryable, asc) => asc ? queryable.OrderByDescending(expression) : queryable.OrderBy(expression),
            (expression, false));
    }

    public GridSort<TGridItem> ThenAscending<U>(Expression<Func<TGridItem, U>> expression)
    {
        then ??= new List<Func<IOrderedQueryable<TGridItem>, bool, IOrderedQueryable<TGridItem>>>();
        thenExpressions ??= new List<(LambdaExpression, bool)>();
        then.Add((queryable, asc) => asc ? queryable.ThenBy(expression) : queryable.ThenByDescending(expression));
        thenExpressions.Add((expression, true));
        cachedPropertyListAscending = null;
        cachedPropertyListDescending = null;
        return this;
    }

    public GridSort<TGridItem> ThenDescending<U>(Expression<Func<TGridItem, U>> expression)
    {
        then ??= new List<Func<IOrderedQueryable<TGridItem>, bool, IOrderedQueryable<TGridItem>>>();
        thenExpressions ??= new List<(LambdaExpression, bool)>();
        then.Add((queryable, asc) => asc ? queryable.ThenByDescending(expression) : queryable.ThenBy(expression));
        thenExpressions.Add((expression, false));
        cachedPropertyListAscending = null;
        cachedPropertyListDescending = null;
        return this;
    }

    internal IOrderedQueryable<TGridItem> Apply(IQueryable<TGridItem> queryable, bool ascending)
    {
        var orderedQueryable = first(queryable, ascending);

        if (then is not null)
        {
            foreach (var clause in then)
            {
                orderedQueryable = clause(orderedQueryable, ascending);
            }
        }

        return orderedQueryable;
    }

    internal IReadOnlyCollection<(string PropertyName, SortDirection Direction)> ToPropertyList(bool ascending)
    {
        if (ascending)
        {
            cachedPropertyListAscending ??= BuildPropertyList(true);
            return cachedPropertyListAscending;
        }

        cachedPropertyListDescending ??= BuildPropertyList(false);
        return cachedPropertyListDescending;
    }

    private IReadOnlyCollection<(string PropertyName, SortDirection Direction)> BuildPropertyList(bool ascending)
    {
        var result = new List<(string, SortDirection)>();
        result.Add((ToPropertyName(firstExpression.Item1),
            firstExpression.Item2 ^ ascending ? SortDirection.Descending : SortDirection.Ascending));

        if (thenExpressions is not null)
        {
            foreach (var (thenLambda, thenAscending) in thenExpressions)
            {
                result.Add((ToPropertyName(thenLambda),
                    thenAscending ^ ascending ? SortDirection.Descending : SortDirection.Ascending));
            }
        }

        return result;
    }

    // Not sure we really want this level of complexity, but it converts expressions like @(c => c.Medals.Gold) to "Medals.Gold"
    private static string ToPropertyName(LambdaExpression expression)
    {
        var body = expression.Body as MemberExpression;
        if (body is null)
        {
            throw new ArgumentException(ExpressionNotRepresentableMessage);
        }

        // Handles cases like @(x => x.Name)
        if (body.Expression is ParameterExpression)
        {
            return body.Member.Name;
        }

        // First work out the length of the string we'll need, so that we can use string.Create
        var length = body.Member.Name.Length;
        var node = body;
        while (node.Expression is not null)
        {
            if (node.Expression is MemberExpression parentMember)
            {
                length += parentMember.Member.Name.Length + 1;
                node = parentMember;
            }
            else if (node.Expression is ParameterExpression)
            {
                break;
            }
            else
            {
                throw new ArgumentException(ExpressionNotRepresentableMessage);
            }
        }

        // Now construct the string
        return string.Create(length, body, (chars, body) =>
        {
            var nextPos = chars.Length;
            while (body is not null)
            {
                nextPos -= body.Member.Name.Length;
                body.Member.Name.CopyTo(chars.Slice(nextPos));
                if (nextPos > 0)
                {
                    chars[--nextPos] = '.';
                }

                body = (body.Expression as MemberExpression)!;
            }
        });
    }
}