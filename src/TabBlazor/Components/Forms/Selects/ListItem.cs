using System;

namespace TabBlazor.Components.Selects
{
    public class ListItem<TItem, TValue>
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; }
        public bool Disabled { get; set; }
        public TValue Value { get; set; }
        public TItem Item { get; set; }
    }
}
