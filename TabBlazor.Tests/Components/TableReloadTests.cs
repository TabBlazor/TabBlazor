using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using TabBlazor.Components.Tables;
using TabBlazor.Components.Tables.Components;

namespace TabBlazor.Tests.Components
{
    public class TableReloadTests : TabBlazorTestContext
    {
        private sealed class CountingProvider : IDataProvider<string>
        {
            public int Calls { get; private set; }

            public Task<IEnumerable<TableResult<object, string>>> GetData(
                List<IColumn<string>> columns, ITableState<string> state, IEnumerable<string> items,
                bool resetPage = false, bool addSorting = true, string moveToItem = default)
            {
                Calls++;
                var rows = (items ?? Enumerable.Empty<string>()).ToList();
                return Task.FromResult<IEnumerable<TableResult<object, string>>>(
                    new[] { new TableResult<object, string>(null, rows) });
            }
        }

        private IRenderedComponent<Table<string>> RenderTable(CountingProvider provider, IList<string> items) =>
            Render<Table<string>>(p => p
                .Add(t => t.DataProvider, provider)
                .Add(t => t.Items, items)
                .AddChildContent<Column<string>>(c => c.Add(x => x.Title, "Value")));

        // Re-applies the given parameters to the table, re-invoking OnParametersSetAsync,
        // as a parent re-render does. Omitted parameters keep their current values.
        private static void ReapplyParameters(IRenderedComponent<Table<string>> cut, IDataProvider<string> provider, IList<string> items) =>
            cut.Render(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                [nameof(Table<string>.DataProvider)] = provider,
                [nameof(Table<string>.Items)] = items,
            }));

        [Fact]
        public void Does_not_refetch_when_parameters_resupplied_unchanged()
        {
            var provider = new CountingProvider();
            var items = new List<string> { "a", "b", "c" };
            var cut = RenderTable(provider, items);
            var afterInit = provider.Calls;

            ReapplyParameters(cut, provider, items);

            Assert.Equal(afterInit, provider.Calls);
        }

        [Fact]
        public void Refetches_when_items_instance_changes()
        {
            var provider = new CountingProvider();
            var cut = RenderTable(provider, new List<string> { "a" });
            var afterInit = provider.Calls;

            ReapplyParameters(cut, provider, new List<string> { "a", "b" });

            Assert.True(provider.Calls > afterInit, $"afterInit={afterInit} now={provider.Calls}");
        }

        [Fact]
        public void Refetches_when_items_count_changes_on_same_instance()
        {
            var provider = new CountingProvider();
            var items = new List<string> { "a" };
            var cut = RenderTable(provider, items);
            var afterInit = provider.Calls;

            items.Add("b");
            ReapplyParameters(cut, provider, items);

            Assert.True(provider.Calls > afterInit, $"afterInit={afterInit} now={provider.Calls}");
        }
    }
}
