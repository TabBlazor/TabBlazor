#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TabBlazor.Tests.Components.DataViews
{
    public class InMemorySourceTests : DataViewSourceTests<InMemorySource<SourceTestItem>>
    {
        protected override InMemorySource<SourceTestItem> CreateSource(IReadOnlyList<SourceTestItem> items) => new(items);

        [Fact]
        public async Task Natural_sort_orders_embedded_numbers_by_value()
        {
            var items = new[] { "item 10", "item 2", "item 1" }
                .Select((n, i) => new SourceTestItem { Id = i, FirstName = n }).ToList();
            var source = new InMemorySource<SourceTestItem>(items, new InMemorySourceOptions { NaturalSort = true });

            var page = await source.GetPageAsync(Request(PageRequest.Offset(0, 10), [new SortSpec("firstName")]), CancellationToken.None);

            Assert.Equal(["item 1", "item 2", "item 10"], page.Items.Select(i => i.FirstName));
        }

        [Fact]
        public async Task Custom_search_expression_is_used_for_matching()
        {
            var column = DataColumnInfoBuilder.Create<SourceTestItem, string>(x => x.LastName, id: "custom",
                searchExpression: (x, term) => x.LastName.StartsWith(term));
            var request = new DataRequest(PageRequest.Offset(0, 100), [], SearchSpec.Parse("Tu", SearchMode.Words, ["custom"]), [], [column]);

            var page = await Source.GetPageAsync(request, CancellationToken.None);

            Assert.Equal([2], page.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task Reads_the_list_on_every_request()
        {
            var items = Seed.Take(3).ToList();
            var source = new InMemorySource<SourceTestItem>(items);
            items.Add(Seed[3]);

            var page = await source.GetPageAsync(Request(PageRequest.Offset(0, 10)), CancellationToken.None);

            Assert.Equal(4, page.TotalCount);
        }

        [Fact]
        public void Request_round_trips_through_json()
        {
            var request = Request(PageRequest.Offset(25, 25), [new SortSpec("lastName", true)],
                SearchSpec.Parse("ada \"odin ab\"", SearchMode.Words, ["firstName", "customer.name"]),
                [new FilterSpec("quantity", FilterOperator.Between, 1, 10)]);

            var json = JsonSerializer.Serialize(request);
            var copy = JsonSerializer.Deserialize<DataRequest>(json)!;

            Assert.DoesNotContain("ValueExpression", json);
            Assert.Equal(25, copy.Page.Skip);
            Assert.Equal("lastName", copy.Sorts[0].Column);
            Assert.Equal(["ada", "odin ab"], copy.Search!.Terms);
            Assert.Equal("customer.name", copy.Columns.Single(c => c.Id == "customer.name").FilterPaths.Single());
            Assert.Equal(DataValueKind.Number, copy.Columns.Single(c => c.Id == "quantity").Kind);
        }

        [Fact]
        public async Task Deserialized_filter_values_are_converted_to_the_column_type()
        {
            var json = JsonSerializer.Serialize(new FilterSpec("quantity", FilterOperator.Between, 1, 10));
            var filter = JsonSerializer.Deserialize<FilterSpec>(json)!;

            var ids = await AllIds(filters: [filter]);

            Assert.Equal([2, 4, 5], ids);
        }
    }

    public class InMemoryCursorSourceTests : DataViewSourceTests<InMemorySource<SourceTestItem>>
    {
        protected override InMemorySource<SourceTestItem> CreateSource(IReadOnlyList<SourceTestItem> items) =>
            new(items, new InMemorySourceOptions { PagingMode = PagingMode.Cursor, IncludeTotalCount = false });
    }

    public class SearchSpecTests
    {
        [Theory]
        [InlineData("odi 2026", new[] { "odi", "2026" })]
        [InlineData("  \"Ada Lovelace\"   2026 ", new[] { "Ada Lovelace", "2026" })]
        [InlineData("a b c d e f g", new[] { "a", "b", "c", "d", "e" })]
        [InlineData("ada ADA", new[] { "ada" })]
        [InlineData("\"open quote", new[] { "open quote" })]
        public void Parses_words_and_quoted_phrases(string text, string[] expected)
        {
            Assert.Equal(expected, SearchSpec.Parse(text, SearchMode.Words, [])!.Terms);
        }

        [Fact]
        public void Phrase_mode_keeps_the_whole_text()
        {
            Assert.Equal(["Ada Love"], SearchSpec.Parse(" Ada Love ", SearchMode.Phrase, [])!.Terms);
        }

        [Fact]
        public void Empty_text_is_no_search()
        {
            Assert.Null(SearchSpec.Parse("  ", SearchMode.Words, []));
            Assert.Null(SearchSpec.Parse("\"\"", SearchMode.Words, []));
        }

        [Fact]
        public void Max_terms_is_configurable()
        {
            Assert.Equal(2, SearchSpec.Parse("a b c", SearchMode.Words, [], maxTerms: 2)!.Terms.Count);
        }
    }

    public class DataColumnInfoBuilderTests
    {
        private sealed class Order
        {
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public DateTime OrderDate { get; set; }
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string? CreatedBy { get; set; }
            public SourceTestCustomer Customer { get; set; } = new();
            public List<int> Lines { get; set; } = [];
        }

        [Fact]
        public void Member_path_becomes_a_camel_case_id()
        {
            var info = DataColumnInfoBuilder.Create<Order, string>(o => o.Customer.Name);

            Assert.Equal("customer.name", info.Id);
            Assert.Equal(["customer.name"], info.SortPaths);
            Assert.Equal(["customer.name"], info.FilterPaths);
            Assert.Equal(FilterPathMode.Exact, info.FilterMode);
            Assert.Equal(DataValueKind.Text, info.Kind);
        }

        [Fact]
        public void Calculated_text_uses_the_members_it_reads()
        {
            var info = DataColumnInfoBuilder.Create<Order, string>(u => $"{u.FirstName} {u.LastName}");

            Assert.Equal("firstName-lastName", info.Id);
            Assert.Equal(["firstName", "lastName"], info.SortPaths);
            Assert.Equal(["firstName", "lastName"], info.FilterPaths);
            Assert.Equal(FilterPathMode.Parts, info.FilterMode);
        }

        [Fact]
        public void Coalesce_and_concat_are_understood()
        {
            Assert.Equal("createdBy-lastName", DataColumnInfoBuilder.Create<Order, string>(o => o.CreatedBy ?? o.LastName).Id);
            Assert.Equal("firstName-lastName", DataColumnInfoBuilder.Create<Order, string>(o => string.Concat(o.FirstName, o.LastName)).Id);
        }

        [Fact]
        public void Calculated_number_has_an_id_but_no_paths()
        {
            var info = DataColumnInfoBuilder.Create<Order, decimal>(o => o.Quantity * o.Price);

            Assert.Equal("quantity-price", info.Id);
            Assert.Empty(info.SortPaths);
            Assert.Empty(info.FilterPaths);
            Assert.Single(info.SortExpressions);
        }

        [Fact]
        public void Members_of_simple_values_are_cut_off()
        {
            var info = DataColumnInfoBuilder.Create<Order, int>(o => o.OrderDate.Year);

            Assert.Equal("orderDate", info.Id);
            Assert.Equal("lines", DataColumnInfoBuilder.Create<Order, int>(o => o.Lines.Count).Id);
        }

        [Fact]
        public void Explicit_id_wins()
        {
            Assert.Equal("fullName", DataColumnInfoBuilder.Create<Order, string>(u => u.FirstName + u.LastName, id: "fullName").Id);
        }

        [Fact]
        public void SortBy_and_FilterBy_override_the_paths()
        {
            var info = DataColumnInfoBuilder.Create<Order, string>(u => u.FirstName + " " + u.LastName,
                sortBy: u => new { u.LastName, u.FirstName }, filterBy: u => u.OrderDate);

            Assert.Equal(["lastName", "firstName"], info.SortPaths);
            Assert.Equal(2, info.SortExpressions.Count);
            Assert.Equal(["orderDate"], info.FilterPaths);
            Assert.Equal(FilterPathMode.Exact, info.FilterMode);
            Assert.Equal(DataValueKind.Date, info.Kind);
            Assert.Equal(typeof(DateTime), info.FilterValueType);
        }

        [Fact]
        public void FilterBy_with_several_members_is_parts()
        {
            var info = DataColumnInfoBuilder.Create<Order, string>(u => u.FirstName,
                filterBy: u => new { u.FirstName, u.LastName });

            Assert.Equal(FilterPathMode.Parts, info.FilterMode);
            Assert.Equal(["firstName", "lastName"], info.FilterPaths);
        }

        [Fact]
        public void Value_without_members_needs_an_id()
        {
            Assert.Throws<InvalidOperationException>(() => DataColumnInfoBuilder.Create<Order, string>(o => "x"));
            Assert.Equal("fallback", DataColumnInfoBuilder.Create<Order, string>(o => "x", fallbackId: "fallback").Id);
        }
    }
}
