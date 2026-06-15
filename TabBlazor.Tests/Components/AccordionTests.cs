using Microsoft.AspNetCore.Components;

namespace TabBlazor.Tests.Components
{
    public class AccordionTests : TabBlazorTestContext
    {
        private static RenderFragment Item(string title, bool expanded = false) => builder =>
        {
            builder.OpenComponent<AccordionItem>(0);
            builder.AddAttribute(1, nameof(AccordionItem.Title), title);
            builder.AddAttribute(2, nameof(AccordionItem.Expanded), expanded);
            builder.AddAttribute(3, nameof(AccordionItem.ChildContent),
                (RenderFragment)(b => b.AddContent(0, $"{title} body")));
            builder.CloseComponent();
        };

        [Fact]
        public void Renders_div_with_accordion_class()
        {
            var cut = Render<Accordion>();
            Assert.Contains("accordion", cut.Find("div").ClassList);
        }

        [Fact]
        public void Renders_an_item_per_accordion_item()
        {
            var cut = Render<Accordion>(p => p.AddChildContent(b =>
            {
                Item("One")(b);
                Item("Two")(b);
            }));

            Assert.Equal(2, cut.FindAll("div.accordion-item").Count);
        }

        [Fact]
        public void Item_button_collapsed_when_not_expanded()
        {
            var cut = Render<Accordion>(p => p.AddChildContent(Item("One")));

            Assert.Contains("collapsed", cut.Find("button.accordion-button").ClassList);
            Assert.DoesNotContain("show", cut.Find("div.accordion-collapse").ClassList);
        }

        [Fact]
        public void Item_shown_when_expanded()
        {
            var cut = Render<Accordion>(p => p.AddChildContent(Item("One", expanded: true)));

            Assert.DoesNotContain("collapsed", cut.Find("button.accordion-button").ClassList);
            Assert.Contains("show", cut.Find("div.accordion-collapse").ClassList);
        }

        [Fact]
        public void Clicking_header_expands_item()
        {
            var cut = Render<Accordion>(p => p.AddChildContent(Item("One")));

            cut.Find("button.accordion-button").Click();

            Assert.Contains("show", cut.Find("div.accordion-collapse").ClassList);
        }

        [Fact]
        public void Opening_one_collapses_other_when_single_open()
        {
            var cut = Render<Accordion>(p => p.AddChildContent(b =>
            {
                Item("One", expanded: true)(b);
                Item("Two")(b);
            }));

            var buttons = cut.FindAll("button.accordion-button");
            buttons[1].Click();

            var collapses = cut.FindAll("div.accordion-collapse");
            Assert.DoesNotContain("show", collapses[0].ClassList);
            Assert.Contains("show", collapses[1].ClassList);
        }
    }
}
