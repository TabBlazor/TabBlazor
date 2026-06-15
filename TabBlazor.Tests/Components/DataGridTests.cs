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
        public void Forwards_unmatched_attributes()
        {
            var cut = Render<DataGrid>(p => p.AddUnmatched("data-test", "value"));

            Assert.Equal("value", cut.Find("div.datagrid").GetAttribute("data-test"));
        }
    }
}
