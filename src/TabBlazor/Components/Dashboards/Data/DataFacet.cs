
namespace TabBlazor.Dashboards
{
    public class DataFacet<TItem> where TItem : class
    {
        public string Name { get; set; }
        public List<FacetFilter<TItem>> Filters { get; set; } = new();
    }

    public class FacetFilter<TItem> where TItem : class
    {
        public DataFilter<TItem> Filter { get; set; }
        public IEnumerable<TItem> Items { get; set; }
        public bool Active { get; set; }
        public int CountAll { get; set; }
        public int CountFiltered { get; set; }

     
    }

}
