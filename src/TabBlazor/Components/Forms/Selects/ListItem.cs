namespace TabBlazor.Components.Selects
{
    public class ListItem<TValue>
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public TValue Item { get; set; }
        public bool Selected { get; set; }

    }
}
