using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;

namespace TabBlazor.Components.Tables.Components
{
    public class TableResult<TKey, TElement> : List<TElement>, IGrouping<TKey, TElement>
    {
        public TableResult(TKey key, List<TElement> elements)
        {
            Key = key;
            AddRange(elements);
        }

        public TKey Key { get; set; }
        public bool Expanded = true;
        public RenderFragment<TableResult<TKey, TElement>> GroupingTemplate { get; set; }
    }
}
