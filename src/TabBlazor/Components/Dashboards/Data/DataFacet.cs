
using System.Reflection.Emit;

namespace TabBlazor.Dashboards
{
    public class DataFacet<TItem> where TItem : class
    {
        public string Name { get; set; }
        public List<FacetFilter<TItem>> Filters { get; set; } = new();
        public bool IsActive => Filters.Any(e => e.Active);

    }

    public class FacetFilter<TItem> where TItem : class
    {
        public DataFilter<TItem> Filter { get; set; }
        public List<TItem> Items { get; set; }
        public IEnumerable<TItem> FilteredItems { get; set; }
        //public string Label { get; set; }
        public bool Active { get; set; }
        public int CountAll { get; set; }
     //   public int CountFiltered { get; set; }
        public Func<FacetFilter<TItem>, string> FilterLabel { get; set; }

        public string GetLabel()
        {
                if (FilterLabel != null)
                {
                    return FilterLabel(this);
                }
                else
                {
                    return $"{Filter.Name} ({CountAll})";
                }
           
        }

        //public void SetLabel()
        //{
        //    if (FilterLabel != null)
        //    {
        //        Label = FilterLabel(this);
        //    }
        //    else
        //    {
        //        Label = $"{Filter.Name} ({CountAll})";
        //    }
        //}


    }

}
