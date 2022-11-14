namespace TabBlazor.Components.QuickTables;

public class PropertyColumn<TGridItem, TProp> : ColumnBase<TGridItem>, ISortBuilderColumn<TGridItem>
{
    private Func<TGridItem, string> _cellTextFunc;
    private Expression<Func<TGridItem, TProp>> _lastAssignedProperty;
    private GridSort<TGridItem> _sortBuilder;

    [Parameter] [EditorRequired] public Expression<Func<TGridItem, TProp>> Property { get; set; } = default!;
    [Parameter] public string Format { get; set; }

    GridSort<TGridItem> ISortBuilderColumn<TGridItem>.SortBuilder => _sortBuilder;

    protected override void OnParametersSet()
    {
        // We have to do a bit of pre-processing on the lambda expression. Only do that if it's new or changed.
        if (_lastAssignedProperty != Property)
        {
            _lastAssignedProperty = Property;
            var compiledPropertyExpression = Property.Compile();

            if (!string.IsNullOrEmpty(Format))
            {
                // TODO: Consider using reflection to avoid having to box every value just to call IFormattable.ToString
                // For example, define a method "string Format<U>(Func<TGridItem, U> property) where U: IFormattable", and
                // then construct the closed type here with U=TProp when we know TProp implements IFormattable

                // If the type is nullable, we're interested in formatting the underlying type
                var nullableUnderlyingTypeOrNull = Nullable.GetUnderlyingType(typeof(TProp));
                if (!typeof(IFormattable).IsAssignableFrom(nullableUnderlyingTypeOrNull ?? typeof(TProp)))
                {
                    throw new InvalidOperationException(
                        $"A '{nameof(Format)}' parameter was supplied, but the type '{typeof(TProp)}' does not implement '{typeof(IFormattable)}'.");
                }

                _cellTextFunc = item => ((IFormattable)compiledPropertyExpression!(item))?.ToString(Format, null);
            }
            else
            {
                _cellTextFunc = item => compiledPropertyExpression!(item)?.ToString();
            }

            _sortBuilder = GridSort<TGridItem>.ByAscending(Property);
        }

        if (Title is null && Property.Body is MemberExpression memberExpression)
        {
            Title = memberExpression.Member.Name;
        }
    }

    protected internal override void CellContent(RenderTreeBuilder builder, TGridItem item)
    {
        builder.AddContent(0, _cellTextFunc!(item));
    }
}