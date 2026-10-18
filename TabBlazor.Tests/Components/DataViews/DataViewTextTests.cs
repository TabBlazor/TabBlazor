#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace TabBlazor.Tests.Components.DataViews
{
    public class DataViewTextTests : TabBlazorTestContext
    {
        private IRenderedComponent<DataView<Row>> RenderView(DataViewText? text = null) =>
            Render<DataView<Row>>(p =>
            {
                p.Add(x => x.Items, Row.Many(30));
                if (text != null)
                {
                    p.Add(x => x.Text, text);
                }
                p.AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Searchable, true));
            });

        private static string SearchPlaceholder(IRenderedComponent<DataView<Row>> cut) =>
            cut.Find("input[type=search]").GetAttribute("placeholder")!;

        [Fact]
        public void Uses_english_defaults()
        {
            var cut = RenderView();

            Assert.Equal("Search…", SearchPlaceholder(cut));
            Assert.Contains("Showing 1–25 of 30", cut.Markup);
        }

        [Fact]
        public void Text_parameter_overrides_strings()
        {
            var cut = RenderView(new DataViewText { Search = "Sök…" });

            Assert.Equal("Sök…", SearchPlaceholder(cut));
        }

        [Fact]
        public void Uses_app_wide_text_from_options()
        {
            Services.Configure<TablerOptions>(o => o.DataViewText = new DataViewText { Search = "Find…" });

            var cut = RenderView();

            Assert.Equal("Find…", SearchPlaceholder(cut));
        }

        [Fact]
        public void Text_parameter_wins_over_options()
        {
            Services.Configure<TablerOptions>(o => o.DataViewText = new DataViewText { Search = "Find…" });

            var cut = RenderView(new DataViewText { Search = "Sök…" });

            Assert.Equal("Sök…", SearchPlaceholder(cut));
        }

        [Fact]
        public void Translates_strings_through_a_resource_localizer()
        {
            Services.AddSingleton<IStringLocalizerFactory>(new FakeLocalizerFactory(new Dictionary<string, string>
            {
                ["Search"] = "Sök…",
                ["ShowingOfTotal"] = "Visar {0}–{1} av {2}",
            }));
            Services.Configure<TablerOptions>(o => o.DataViewTextResource = typeof(TestResource));

            var cut = RenderView();

            Assert.Equal("Sök…", SearchPlaceholder(cut));
            Assert.Contains("Visar 1–25 av 30", cut.Markup);
        }

        [Fact]
        public void FromLocalizer_keeps_the_fallback_for_missing_keys()
        {
            var localizer = new FakeLocalizer(new Dictionary<string, string> { ["Search"] = "Sök…" });

            var text = DataViewText.FromLocalizer(localizer, new DataViewText { NoData = "Inga rader" });

            Assert.Equal("Sök…", text.Search);
            Assert.Equal("Inga rader", text.NoData);
            Assert.Equal("Retry", text.Retry);
        }

        [Fact]
        public void FromLocalizer_does_not_change_the_fallback()
        {
            var localizer = new FakeLocalizer(new Dictionary<string, string> { ["Search"] = "Sök…" });

            DataViewText.FromLocalizer(localizer);

            Assert.Equal("Search…", DataViewText.Default.Search);
        }

        [Fact]
        public void Resource_text_is_looked_up_per_ui_culture()
        {
            var factory = new CultureLocalizerFactory();
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("sv-SE");
                var sv = DataViewText.FromResource(factory, typeof(CultureResource), DataViewText.Default);
                CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
                var de = DataViewText.FromResource(factory, typeof(CultureResource), DataViewText.Default);

                Assert.Equal("sv-SE", sv.Search);
                Assert.Equal("de-DE", de.Search);
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }

        private sealed class TestResource;

        private sealed class CultureResource;

        private sealed class FakeLocalizerFactory(IReadOnlyDictionary<string, string> strings) : IStringLocalizerFactory
        {
            public IStringLocalizer Create(Type resourceSource) => new FakeLocalizer(strings);
            public IStringLocalizer Create(string baseName, string location) => new FakeLocalizer(strings);
        }

        private sealed class CultureLocalizerFactory : IStringLocalizerFactory
        {
            public IStringLocalizer Create(Type resourceSource) =>
                new FakeLocalizer(new Dictionary<string, string> { ["Search"] = CultureInfo.CurrentUICulture.Name });

            public IStringLocalizer Create(string baseName, string location) => Create(typeof(object));
        }

        private sealed class FakeLocalizer(IReadOnlyDictionary<string, string> strings) : IStringLocalizer
        {
            public LocalizedString this[string name] => strings.TryGetValue(name, out var value)
                ? new LocalizedString(name, value)
                : new LocalizedString(name, name, resourceNotFound: true);

            public LocalizedString this[string name, params object[] arguments] => this[name];

            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
                strings.Select(s => new LocalizedString(s.Key, s.Value));
        }
    }
}
