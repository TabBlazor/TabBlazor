

namespace TabBlazor.Dashboards
{
    public class BaseFacet<TItem> : ComponentBase where TItem : class
    {
        [CascadingParameter] public Dashboard<TItem> Dashboard { get; set; }
        [Parameter] public string Name { get; set; }
        [Parameter] public bool Collapsed { get; set; }
        [Parameter] public RenderFragment<DataFacet<TItem>> FacetTemplate { get; set; }

        [Parameter] public Func<FacetFilter<TItem>, string> FilterLabel { get; set; }

        [Parameter] public Func<IEnumerable<FacetFilter<TItem>>, IEnumerable<FacetFilter<TItem>>> SortFilters { get; set; }

        [Parameter] public int JockeIndex { get; set; } = 0;


        public bool IsExpanded;
        public DataFacet<TItem> Facet;

        protected override async Task OnInitializedAsync()
        {
       
            await base.OnInitializedAsync();
        }

        protected override void OnInitialized()
        {
            IsExpanded = !Collapsed;
            base.OnInitialized();
        }

      
        public IIconType GetExpandedIcon()
        {
            if(IsExpanded == true) { return InternalIcons.Chevron_up; }
            return InternalIcons.Chevron_down;
        }

        public void ToogleExpanded()
        {
            IsExpanded = !IsExpanded;
        }


        public void ResetFilters(bool runFilter)
        {
            foreach (var filter in Facet.Filters.Where(e => e.Active))
            {
                filter.Active = false;
            }
            if (runFilter)
            {
                Dashboard.RunFilter();
            }

        }

    }
}
