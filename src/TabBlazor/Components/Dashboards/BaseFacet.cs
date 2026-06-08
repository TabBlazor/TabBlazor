

namespace TabBlazor.Dashboards
{
    /// <summary>
    /// Base class for dashboard facets — collapsible filter groups that contribute predicates to a
    /// <see cref="Dashboard{TItem}"/>.
    /// </summary>
    public class BaseFacet<TItem> : ComponentBase where TItem : class
    {
        /// <summary>The owning dashboard, supplied via cascading parameter.</summary>
        [CascadingParameter] public Dashboard<TItem> Dashboard { get; set; }
        /// <summary>The facet's display name.</summary>
        [Parameter] public string Name { get; set; }
        /// <summary>Whether the facet starts collapsed. Defaults to false.</summary>
        [Parameter] public bool Collapsed { get; set; }
        /// <summary>Optional custom rendering for the facet, receiving its data.</summary>
        [Parameter] public RenderFragment<DataFacet<TItem>> FacetTemplate { get; set; }

        /// <summary>Projects a filter to its display label.</summary>
        [Parameter] public Func<FacetFilter<TItem>, string> FilterLabel { get; set; }

        /// <summary>Optional custom ordering of the facet's filters.</summary>
        [Parameter] public Func<IEnumerable<FacetFilter<TItem>>, IEnumerable<FacetFilter<TItem>>> SortFilters { get; set; }


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
