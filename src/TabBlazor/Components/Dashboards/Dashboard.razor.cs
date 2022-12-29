

using TabBlazor.Data;

namespace TabBlazor.Dashboard
{
    public partial class Dashboard<TItem> where TItem : class
    {
        [Parameter] public IEnumerable<TItem> Items { get; set; }
        [Parameter] public RenderFragment<DataFactory<TItem>> ChildContent { get; set; }

        private DataFactory<TItem> dataFactory;

        protected override void OnInitialized()
        {
            dataFactory = new DataFactory<TItem>(Items);

           
        }


    }
}