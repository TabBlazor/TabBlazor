namespace TabBlazor.Dashboards
{
    public class DataFilter<TItem> where TItem : class
    {
        public string Name { get; set; }
        public Expression<Func<TItem, bool>> Expression { get; set; }
    }
}
