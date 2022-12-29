

using TabBlazor.Data;

namespace TabBlazor.Dashboard
{
    public partial class Dashboard<TItem> where TItem : class
    {
        [Parameter] public IEnumerable<TItem> Items { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public EventCallback FilteredDataUpdated { get; set; }

        public DataFactory<TItem> DataFactory => dataFactory;



        private DataFactory<TItem> dataFactory;

        protected override void OnInitialized()
        {
            dataFactory = new DataFactory<TItem>(Items);
            dataFactory.OnDataFilter += OnDataFilter;



        }

        private void OnDataFilter()
        {
            FilteredDataUpdated.InvokeAsync();
        }
    }
}