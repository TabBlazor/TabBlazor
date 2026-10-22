using Microsoft.AspNetCore.Components;

namespace TabBlazor.Tests.Components
{
    public class DataGridTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_root_div_with_datagrid_class()
        {
            var cut = Render<DataGrid>();

            var root = cut.Find("div.datagrid");
            Assert.NotNull(root);
        }

        [Fact]
        public void Renders_child_content()
        {
            var cut = Render<DataGrid>(p => p.AddChildContent("<span>Body</span>"));

            Assert.Equal("Body", cut.Find("div.datagrid").TextContent.Trim());
        }

        [Fact]
        public void Adds_background_color_class()
        {
            var cut = Render<DataGrid>(p => p.Add(g => g.BackgroundColor, TablerColor.Primary));

            Assert.Contains("bg-primary", cut.Find("div.datagrid").ClassList);
        }

        [Fact]
        public void Invokes_on_click()
        {
            var clicked = false;
            var cut = Render<DataGrid>(p => p
                .Add(g => g.OnClick, EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => clicked = true)));

            cut.Find("div.datagrid").Click();

            Assert.True(clicked);
        }

        [Fact]
        public void Adds_horizontal_class()
        {
            var cut = Render<DataGrid>(p => p.Add(g => g.Horizontal, true));

            Assert.Contains("datagrid-horizontal", cut.Find("div.datagrid").ClassList);
        }

        [Theory]
        [InlineData(2, "datagrid-cols-2")]
        [InlineData(0, "datagrid-cols-1")]
        [InlineData(9, "datagrid-cols-4")]
        public void Adds_clamped_columns_class(int columns, string expected)
        {
            var cut = Render<DataGrid>(p => p.Add(g => g.Columns, columns));

            Assert.Contains(expected, cut.Find("div.datagrid").ClassList);
        }

        [Fact]
        public void Omits_columns_class_by_default()
        {
            var cut = Render<DataGrid>();

            Assert.DoesNotContain(cut.Find("div.datagrid").ClassList, c => c.StartsWith("datagrid-cols-"));
        }

        [Fact]
        public void Sets_title_width_variable_after_provided_style()
        {
            var cut = Render<DataGrid>(p => p
                .Add(g => g.TitleWidth, "6rem")
                .AddUnmatched("style", "color: red;"));

            Assert.Equal("color: red; --tblr-datagrid-title-width: 6rem;", cut.Find("div.datagrid").GetAttribute("style"));
        }

        [Fact]
        public void Separates_title_width_from_provided_style_without_semicolon()
        {
            var cut = Render<DataGrid>(p => p
                .Add(g => g.TitleWidth, "6rem")
                .AddUnmatched("style", "color: red"));

            Assert.Equal("color: red; --tblr-datagrid-title-width: 6rem;", cut.Find("div.datagrid").GetAttribute("style"));
        }

        [Fact]
        public void Drops_provided_style_when_parent_removes_it()
        {
            var cut = Render<DataGrid>(p => p
                .Add(g => g.TitleWidth, "6rem")
                .AddUnmatched("style", "color: red;"));

            cut.Render(p => p.Add(g => g.TitleWidth, "6rem"));

            Assert.Equal("--tblr-datagrid-title-width: 6rem;", cut.Find("div.datagrid").GetAttribute("style"));
        }

        [Fact]
        public void Forwards_unmatched_attributes()
        {
            var cut = Render<DataGrid>(p => p.AddUnmatched("data-test", "value"));

            Assert.Equal("value", cut.Find("div.datagrid").GetAttribute("data-test"));
        }
    }
}
