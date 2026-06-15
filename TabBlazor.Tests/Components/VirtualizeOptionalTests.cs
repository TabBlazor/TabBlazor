using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace TabBlazor.Tests.Components
{
    public class VirtualizeOptionalTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_each_item_when_not_virtualized()
        {
            var cut = Render<VirtualizeOptional<string>>(p => p
                .Add(v => v.Items, new List<string> { "one", "two" })
                .Add(v => v.ChildContent, (RenderFragment<string>)(item => builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddContent(1, item);
                    builder.CloseElement();
                })));

            Assert.Equal(2, cut.FindAll("span").Count);
            Assert.Contains("one", cut.Markup);
            Assert.Contains("two", cut.Markup);
        }

        [Fact]
        public void Renders_nothing_for_empty_items()
        {
            var cut = Render<VirtualizeOptional<string>>(p => p
                .Add(v => v.Items, new List<string>())
                .Add(v => v.ChildContent, (RenderFragment<string>)(item => builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddContent(1, item);
                    builder.CloseElement();
                })));

            Assert.Empty(cut.FindAll("span"));
        }

        [Fact]
        public void Renders_template_markup_for_item()
        {
            var cut = Render<VirtualizeOptional<int>>(p => p
                .Add(v => v.Items, new List<int> { 42 })
                .Add(v => v.ChildContent, (RenderFragment<int>)(item => builder =>
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "class", "row-item");
                    builder.AddContent(2, item);
                    builder.CloseElement();
                })));

            var item = cut.Find("div.row-item");
            Assert.Equal("42", item.TextContent);
        }
    }
}
