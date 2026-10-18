#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace TabBlazor.Tests.Components.DataViews
{
    public enum SourceTestStatus
    {
        Open,
        [Display(Name = "On hold")] OnHold,
        Closed
    }

    public sealed class SourceTestCustomer
    {
        public string Name { get; set; } = "";
    }

    public sealed class SourceTestItem
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public SourceTestCustomer? Customer { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime Date { get; set; }
        public bool Active { get; set; }
        public SourceTestStatus Status { get; set; }
        public Guid Code { get; set; }
        public string? Note { get; set; }

        public static IReadOnlyList<SourceTestItem> Seed()
        {
            var items = new List<SourceTestItem>
            {
                new() { Id = 1, FirstName = "Ada", LastName = "Lovelace", Customer = new() { Name = "Odin AB" }, Quantity = 2026, Price = 10.50m, Date = new DateTime(2026, 1, 15), Active = true, Status = SourceTestStatus.Open, Code = Guid.Parse("11111111-1111-1111-1111-111111111111"), Note = "vip" },
                new() { Id = 2, FirstName = "Alan", LastName = "Turing", Customer = new() { Name = "Acme" }, Quantity = 5, Price = 99.99m, Date = new DateTime(2025, 6, 1), Active = false, Status = SourceTestStatus.Closed, Code = Guid.Parse("22222222-2222-2222-2222-222222222222") },
                new() { Id = 3, FirstName = "Grace", LastName = "Hopper", Customer = new() { Name = "odin trading" }, Quantity = 12, Price = 12.00m, Date = new DateTime(2024, 3, 10), Active = true, Status = SourceTestStatus.OnHold, Code = Guid.Parse("33333333-3333-3333-3333-333333333333"), Note = "" },
                new() { Id = 4, FirstName = "Ada", LastName = "Byron", Customer = new() { Name = "Acme" }, Quantity = 7, Price = 7.25m, Date = new DateTime(2026, 2, 1), Active = true, Status = SourceTestStatus.Open, Code = Guid.Parse("44444444-4444-4444-4444-444444444444") },
                new() { Id = 5, FirstName = "Linus", LastName = "Torvalds", Customer = null, Quantity = 1, Price = 1000m, Date = new DateTime(2023, 12, 31), Active = false, Status = SourceTestStatus.Open, Code = Guid.Parse("55555555-5555-5555-5555-555555555555"), Note = "kernel" },
            };

            for (var i = 6; i <= 30; i++)
            {
                items.Add(new SourceTestItem
                {
                    Id = i,
                    FirstName = $"Person{i}",
                    LastName = $"Test{i:00}",
                    Customer = new SourceTestCustomer { Name = $"Customer {i % 5}" },
                    Quantity = i * 3,
                    Price = i * 1.5m,
                    Date = new DateTime(2020, 1, 1).AddDays(i),
                    Active = i % 2 == 0,
                    Status = (SourceTestStatus)(i % 3),
                    Code = new Guid(i, 0, 0, new byte[8]),
                    Note = i % 4 == 0 ? null : $"note {i}"
                });
            }

            return items;
        }
    }

    /// <summary>
    /// The columns the shared source tests use, built from member names on <typeparamref name="TItem"/> the same way
    /// DataColumn builds them, so a source over another row type with the same members (a generated GraphQL type)
    /// gets the same columns.
    /// </summary>
    public sealed class SourceTestColumns<TItem>
    {
        private static readonly ParameterExpression Item = Expression.Parameter(typeof(TItem), "x");

        public DataColumnInfo Id { get; } = Member("Id");
        public DataColumnInfo FirstName { get; } = Member("FirstName");
        public DataColumnInfo LastName { get; } = Member("LastName");
        public DataColumnInfo CustomerName { get; } = Member("Customer.Name");
        public DataColumnInfo Quantity { get; } = Member("Quantity");
        public DataColumnInfo Price { get; } = Member("Price");
        public DataColumnInfo Date { get; } = Member("Date");
        public DataColumnInfo Active { get; } = Member("Active");
        public DataColumnInfo Status { get; } = Member("Status");
        public DataColumnInfo Code { get; } = Member("Code");
        public DataColumnInfo Note { get; } = Member("Note");

        /// <summary>A calculated text column with inferred parts: <c>x.FirstName + " " + x.LastName</c>.</summary>
        public DataColumnInfo FullName { get; } = DataColumnInfoBuilder.Create(typeof(string),
            Expression.Lambda(Concat(Path("FirstName"), Expression.Constant(" "), Path("LastName")), Item), "fullName");

        /// <summary>A calculated text column with SortBy (last, first) and FilterBy (first, last) overrides.</summary>
        public DataColumnInfo FullNameBy { get; } = DataColumnInfoBuilder.Create(typeof(string),
            Expression.Lambda(Concat(Path("FirstName"), Expression.Constant(" "), Path("LastName")), Item), "fullNameBy",
            sortBy: Parts("LastName", "FirstName"),
            filterBy: Parts("FirstName", "LastName"));

        /// <summary>A calculated text column that sorts and filters on one stand-in property (the date).</summary>
        public DataColumnInfo Ordered { get; } = DataColumnInfoBuilder.Create(typeof(string),
            Expression.Lambda(Concat(Expression.Call(Path("Date"), "ToString", null, Expression.Constant("d")), Expression.Constant(" "), Path("LastName")), Item),
            "ordered",
            sortBy: Expression.Lambda(Path("Date"), Item),
            filterBy: Expression.Lambda(Path("Date"), Item));

        public IReadOnlyList<DataColumnInfo> All =>
        [
            Id, FirstName, LastName, CustomerName, Quantity, Price, Date, Active, Status, Code, Note, FullName,
            FullNameBy, Ordered
        ];

        private static Expression Path(string path) =>
            path.Split('.').Aggregate((Expression)Item, Expression.PropertyOrField);

        private static DataColumnInfo Member(string path)
        {
            var body = Path(path);
            return DataColumnInfoBuilder.Create(body.Type, Expression.Lambda(body, Item));
        }

        private static Expression Concat(params Expression[] parts) =>
            Expression.Call(typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string), typeof(string)])!, parts);

        private static LambdaExpression Parts(params string[] paths) =>
            Expression.Lambda(Expression.NewArrayInit(typeof(object), paths.Select(p => Expression.Convert(Path(p), typeof(object)))), Item);
    }

    /// <summary>
    /// Shared tests every <see cref="IDataViewSource{TItem}"/> must pass, so in-memory, EF, GraphQL and custom
    /// sources behave alike. Inherit, implement <see cref="CreateSource"/> over the seed (and <see cref="IdOf"/>), and run.
    /// </summary>
    public abstract class DataViewSourceTests<TSource, TItem> where TSource : IDataViewSource<TItem>
    {
        protected IReadOnlyList<SourceTestItem> Seed { get; } = SourceTestItem.Seed();

        protected SourceTestColumns<TItem> Columns { get; } = new();

        /// <summary>Creates the source under test over <paramref name="items"/>.</summary>
        protected abstract TSource CreateSource(IReadOnlyList<SourceTestItem> items);

        /// <summary>The seed id of a row.</summary>
        protected abstract int IdOf(TItem item);

        /// <summary>True when text search ignores case (in memory always; remote sources follow their collation).</summary>
        protected virtual bool SearchIsCaseInsensitive => true;

        private TSource source = default!;

        protected TSource Source => source ??= CreateSource(Seed);

        protected PageRequest FirstPage(int size = 100) =>
            Source.PagingMode == PagingMode.Cursor ? PageRequest.Cursor(null, size) : PageRequest.Offset(0, size);

        protected DataRequest Request(PageRequest page, IReadOnlyList<SortSpec>? sorts = null,
            SearchSpec? search = null, IReadOnlyList<FilterSpec>? filters = null) =>
            new(page, sorts ?? [new SortSpec("id")], search, filters ?? [], Columns.All);

        protected static SearchSpec? Search(string text, params DataColumnInfo[] columns) =>
            SearchSpec.Parse(text, SearchMode.Words, columns.Select(c => c.Id).ToList());

        protected async Task<List<int>> Ids(DataRequest request)
        {
            var page = await Source.GetPageAsync(request, CancellationToken.None);
            return page.Items.Select(IdOf).ToList();
        }

        protected async Task<List<int>> AllIds(IReadOnlyList<SortSpec>? sorts = null, SearchSpec? search = null,
            IReadOnlyList<FilterSpec>? filters = null) =>
            await Ids(Request(FirstPage(), sorts, search, filters));

        // Paging

        [Fact]
        public async Task Offset_paging_returns_the_requested_window()
        {
            if (Source.PagingMode != PagingMode.Offset)
            {
                return;
            }

            var ids = await Ids(Request(PageRequest.Offset(5, 10)));

            Assert.Equal(Enumerable.Range(6, 10), ids);
        }

        [Fact]
        public async Task Offset_paging_past_the_end_returns_an_empty_page()
        {
            if (Source.PagingMode != PagingMode.Offset)
            {
                return;
            }

            var page = await Source.GetPageAsync(Request(PageRequest.Offset(100, 10)), CancellationToken.None);

            Assert.Empty(page.Items);
        }

        [Fact]
        public async Task Cursor_paging_walks_every_item_once()
        {
            if (Source.PagingMode != PagingMode.Cursor)
            {
                return;
            }

            var ids = new List<int>();
            string? cursor = null;
            for (var i = 0; i < 10; i++)
            {
                var page = await Source.GetPageAsync(Request(PageRequest.Cursor(cursor, 7)), CancellationToken.None);
                ids.AddRange(page.Items.Select(IdOf));
                cursor = page.NextCursor;
                if (cursor == null)
                {
                    break;
                }
            }

            Assert.Equal(Enumerable.Range(1, 30), ids);
        }

        [Fact]
        public async Task Total_count_is_the_number_of_matching_rows_when_reported()
        {
            var all = await Source.GetPageAsync(Request(FirstPage(5)), CancellationToken.None);
            var searched = await Source.GetPageAsync(
                Request(FirstPage(5), search: Search("Ada", Columns.FirstName)), CancellationToken.None);

            Assert.Equal(5, all.Items.Count);
            Assert.True(all.TotalCount is null or 30);
            Assert.True(searched.TotalCount is null or 2);
        }

        // Sorting

        [Fact]
        public async Task Sorts_text_ascending_and_descending()
        {
            var expected = Seed.OrderBy(x => x.LastName, StringComparer.Ordinal).Select(x => x.Id).ToList();

            Assert.Equal(expected, await AllIds([new SortSpec("lastName")]));
            expected.Reverse();
            Assert.Equal(expected, await AllIds([new SortSpec("lastName", true)]));
        }

        [Fact]
        public async Task Sorts_numbers_and_dates()
        {
            Assert.Equal(Seed.OrderBy(x => x.Price).ThenBy(x => x.Id).Select(x => x.Id),
                await AllIds([new SortSpec("price"), new SortSpec("id")]));
            Assert.Equal(Seed.OrderByDescending(x => x.Date).Select(x => x.Id), await AllIds([new SortSpec("date", true)]));
        }

        [Fact]
        public async Task Sorts_by_a_nested_path_with_nulls_first()
        {
            var ids = await AllIds([new SortSpec("customer.name"), new SortSpec("id")]);

            Assert.Equal(5, ids[0]);
            Assert.Equal(30, ids.Count);
        }

        [Fact]
        public async Task Sorts_a_calculated_text_column()
        {
            var expected = Seed.OrderBy(x => x.FirstName, StringComparer.Ordinal).ThenBy(x => x.LastName, StringComparer.Ordinal).Select(x => x.Id);

            Assert.Equal(expected, await AllIds([new SortSpec("fullName")]));
        }

        [Fact]
        public async Task Sorts_by_SortBy_parts_in_order()
        {
            var expected = Seed.OrderBy(x => x.LastName, StringComparer.Ordinal).ThenBy(x => x.FirstName, StringComparer.Ordinal).Select(x => x.Id);

            Assert.Equal(expected, await AllIds([new SortSpec("fullNameBy")]));
        }

        [Fact]
        public async Task Sorts_by_a_SortBy_stand_in()
        {
            Assert.Equal(Seed.OrderBy(x => x.Date).Select(x => x.Id), await AllIds([new SortSpec("ordered")]));
        }

        [Fact]
        public async Task Unknown_sort_column_is_an_error()
        {
            await Assert.ThrowsAnyAsync<Exception>(() =>
                Source.GetPageAsync(Request(FirstPage(), [new SortSpec("doesNotExist")]), CancellationToken.None));
        }

        // Search

        [Fact]
        public async Task Search_matches_every_word_in_any_searchable_column()
        {
            var ids = await AllIds(search: Search("Odi 2026", Columns.CustomerName, Columns.Quantity));

            Assert.Equal([1], ids);
        }

        [Fact]
        public async Task Search_ignores_case()
        {
            if (!SearchIsCaseInsensitive)
            {
                return;
            }

            Assert.Equal([1, 4], await AllIds(search: Search("aDA", Columns.FirstName)));
        }

        [Fact]
        public async Task Search_keeps_quoted_phrases_together()
        {
            var search = Search("\"Odin AB\"", Columns.CustomerName);

            Assert.Equal(["Odin AB"], search!.Terms);
            Assert.Equal([1], await AllIds(search: search));
        }

        [Fact]
        public async Task Search_matches_numbers_by_value_not_by_text()
        {
            Assert.Equal([3], await AllIds(search: Search("12", Columns.Quantity)));
        }

        [Fact]
        public async Task Search_matches_enums_by_display_name()
        {
            var expected = Seed.Where(x => x.Status == SourceTestStatus.OnHold).Select(x => x.Id);

            Assert.Equal(expected, await AllIds(search: Search("hold", Columns.Status)));
        }

        [Fact]
        public async Task Search_matches_guids_exactly()
        {
            Assert.Equal([2], await AllIds(search: Search("22222222-2222-2222-2222-222222222222", Columns.Code)));
        }

        [Fact]
        public async Task Search_does_not_match_dates_or_bools()
        {
            Assert.Empty(await AllIds(search: Search("2026", Columns.Date, Columns.Active)));
        }

        [Fact]
        public async Task Search_matches_words_across_parts_of_a_calculated_column()
        {
            Assert.Equal([1], await AllIds(search: Search("Ada Love", Columns.FullName)));
            Assert.Equal([1], await AllIds(search: Search("Love Ada", Columns.FullNameBy)));
        }

        [Fact]
        public async Task Search_handles_null_values()
        {
            var ids = await AllIds(search: Search("kernel", Columns.CustomerName, Columns.Note));

            Assert.Equal([5], ids);
        }

        [Fact]
        public async Task Phrase_search_matches_the_whole_text()
        {
            var search = SearchSpec.Parse("Ada Love", SearchMode.Phrase, ["firstName", "lastName"]);

            Assert.Empty(await AllIds(search: search));
        }

        // Filters

        [Fact]
        public async Task Filters_text_contains_and_equals()
        {
            Assert.Equal([1, 3], await AllIds(filters: [new FilterSpec("customer.name", FilterOperator.Contains, "din")]));
            Assert.Equal([2, 4], await AllIds(filters: [new FilterSpec("customer.name", FilterOperator.Equals, "Acme")]));
        }

        [Fact]
        public async Task Filters_numbers_with_ranges()
        {
            var expected = Seed.Where(x => x.Quantity >= 12 && x.Quantity <= 30).Select(x => x.Id);

            Assert.Equal(expected, await AllIds(filters: [new FilterSpec("quantity", FilterOperator.Between, 12, 30)]));
            Assert.Equal([1], await AllIds(filters: [new FilterSpec("quantity", FilterOperator.GreaterThan, 1000)]));
        }

        [Fact]
        public async Task Filters_enums_with_in()
        {
            var expected = Seed.Where(x => x.Status is SourceTestStatus.OnHold or SourceTestStatus.Closed).Select(x => x.Id);

            Assert.Equal(expected, await AllIds(filters:
                [new FilterSpec("status", FilterOperator.In, new[] { "OnHold", "Closed" })]));
        }

        [Fact]
        public async Task Filters_null_and_not_null()
        {
            // A null value on a nullable field (servers reject "is null" on non-nullable fields).
            Assert.Equal(Seed.Where(x => x.Note == null).Select(x => x.Id),
                await AllIds(filters: [new FilterSpec("note", FilterOperator.IsNull)]));
            Assert.Equal(Seed.Where(x => x.Note != null).Select(x => x.Id),
                await AllIds(filters: [new FilterSpec("note", FilterOperator.IsNotNull)]));
        }

        [Fact]
        public async Task Filters_on_a_FilterBy_stand_in_use_its_type()
        {
            var expected = Seed.Where(x => x.Date >= new DateTime(2026, 1, 1)).Select(x => x.Id);

            Assert.Equal(expected, await AllIds(filters:
                [new FilterSpec("ordered", FilterOperator.GreaterThanOrEqual, new DateTime(2026, 1, 1))]));
        }

        [Fact]
        public async Task Filters_on_parts_match_each_word_in_one_part()
        {
            Assert.Equal([1], await AllIds(filters: [new FilterSpec("fullNameBy", FilterOperator.Contains, "Love Ada")]));
        }

        [Fact]
        public async Task Filters_and_search_are_combined()
        {
            var ids = await AllIds(
                search: Search("Ada", Columns.FirstName),
                filters: [new FilterSpec("customer.name", FilterOperator.Equals, "Acme")]);

            Assert.Equal([4], ids);
        }

        // Lifecycle

        [Fact]
        public async Task Cancelled_requests_throw()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                Source.GetPageAsync(Request(FirstPage()), cts.Token));
        }
    }

    /// <summary>The shared source tests over <see cref="SourceTestItem"/> rows.</summary>
    public abstract class DataViewSourceTests<TSource> : DataViewSourceTests<TSource, SourceTestItem>
        where TSource : IDataViewSource<SourceTestItem>
    {
        protected override int IdOf(SourceTestItem item) => item.Id;
    }
}
