using System.Linq;
using TabBlazor.Components.QuickTables;

namespace TabBlazor.Tests.Components
{
    public class QuickTableTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_responsive_wrapper_around_quick_table()
        {
            var cut = Render<QuickTable<string>>(p => p
                .Add(t => t.Items, new[] { "a", "b" }.AsQueryable()));

            var wrapper = cut.Find("div.table-responsive");
            Assert.NotNull(wrapper);
            Assert.Single(cut.FindAll("table.quick-table"));
        }

        [Fact]
        public void Renders_default_theme_attribute()
        {
            var cut = Render<QuickTable<string>>(p => p
                .Add(t => t.Items, new[] { "a" }.AsQueryable()));

            Assert.Equal("default", cut.Find("table.quick-table").GetAttribute("theme"));
        }

        [Fact]
        public void Applies_custom_theme_attribute()
        {
            var cut = Render<QuickTable<string>>(p => p
                .Add(t => t.Theme, "dark")
                .Add(t => t.Items, new[] { "a" }.AsQueryable()));

            Assert.Equal("dark", cut.Find("table.quick-table").GetAttribute("theme"));
        }

        [Fact]
        public void Applies_custom_class_to_wrapper()
        {
            var cut = Render<QuickTable<string>>(p => p
                .Add(t => t.Class, "my-grid")
                .Add(t => t.Items, new[] { "a" }.AsQueryable()));

            Assert.Contains("my-grid", cut.Find("div.table-responsive").ClassList);
        }

        [Fact]
        public void Renders_header_row()
        {
            var cut = Render<QuickTable<string>>(p => p
                .Add(t => t.Items, new[] { "a" }.AsQueryable()));

            Assert.Single(cut.FindAll("thead tr"));
        }
    }
}
