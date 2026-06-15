using Microsoft.AspNetCore.Components;

namespace TabBlazor.Tests.Components
{
    public class DataGridItemTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_root_div_with_datagrid_item_class()
        {
            var cut = Render<DataGridItem>();

            Assert.NotNull(cut.Find("div.datagrid-item"));
        }

        [Fact]
        public void Renders_title_text()
        {
            var cut = Render<DataGridItem>(p => p.Add(i => i.Title, "Label"));

            Assert.Contains("Label", cut.Find("div.datagrid-title").TextContent);
        }

        [Fact]
        public void Renders_title_template_over_title()
        {
            var cut = Render<DataGridItem>(p => p
                .Add(i => i.Title, "Ignored")
                .Add(i => i.TitleTemplate, (RenderFragment)(b => b.AddContent(0, "Custom"))));

            var title = cut.Find("div.datagrid-title");
            Assert.Contains("Custom", title.TextContent);
            Assert.DoesNotContain("Ignored", title.TextContent);
        }

        [Fact]
        public void Renders_child_content_in_content_section()
        {
            var cut = Render<DataGridItem>(p => p.AddChildContent("<span>Value</span>"));

            Assert.Equal("Value", cut.Find("div.datagrid-content").TextContent.Trim());
        }

        [Fact]
        public void Adds_text_color_class()
        {
            var cut = Render<DataGridItem>(p => p.Add(i => i.TextColor, TablerColor.Danger));

            Assert.Contains("text-danger", cut.Find("div.datagrid-item").ClassList);
        }
    }
}
