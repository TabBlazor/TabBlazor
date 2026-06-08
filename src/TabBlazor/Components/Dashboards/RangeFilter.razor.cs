

namespace TabBlazor.Dashboards
{
    /// <summary>A dashboard filter with min/max inputs that constrains a numeric property to a range.</summary>
    public partial class RangeFilter<TItem> : DashboardComponent<TItem> where TItem : class
    {
        /// <summary>The numeric property to constrain.</summary>
        [Parameter] public Expression<Func<TItem, decimal>> Expression { get; set; }
        /// <summary>The filter's display name.</summary>
        [Parameter] public string Name { get; set; }

        private decimal allMin;
        private decimal allMax;

        private decimal min;
        private decimal max;

        /// <summary>Optional custom content rendered for the filter.</summary>
        [Parameter] public RenderFragment<DataFacet<TItem>> Facet { get; set; }
     
        private DataFilter<TItem> filter;

        protected override void OnInitialized()
        {
            allMin = Dashboard.AllItems.Min(Expression);
            allMax = Dashboard.AllItems.Max(Expression);

            min = allMin;
            max = allMax;
     
        }

        private void RemoveFilter()
        {
            min = allMin;
            max = allMax;
            Dashboard.RemoveFilter(filter);
            filter = null;

        }

        private void MinUpdated(ChangeEventArgs e)
        {
            if (decimal.TryParse(e.Value.ToString(), out var inputValue))
            {
                if (inputValue < allMin) {  inputValue = allMin; }

                min = inputValue;
                FilterData();
          
            }
        }

        private void MaxUpdated(ChangeEventArgs e)
        {
            if (decimal.TryParse(e.Value.ToString(), out var inputValue))
            {
                if (inputValue > allMax) {  inputValue= allMax; }

                max = inputValue;
                FilterData();
            }
        }
             


        private void FilterData()
        {
            var predicate = FacetsHelper.CreateRangePredicate(Expression, min, max);

            if (filter == null)
            {
                filter = new DataFilter<TItem>
                {
                    Expression = predicate,
                    Name = Name
                };

                Dashboard.AddFilter(filter);
            }
            else
            {
                filter.Expression = predicate;
                Dashboard.RunFilter();
            }


        }


    }
}
