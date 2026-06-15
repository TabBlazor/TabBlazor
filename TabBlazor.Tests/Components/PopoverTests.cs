namespace TabBlazor.Tests.Components
{
    public class PopoverTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_container_with_trigger_content()
        {
            var cut = Render<Popover>(p => p.AddChildContent("Open me"));

            var container = cut.Find("div.popover-container");
            Assert.Contains("Open me", container.TextContent);
        }

        [Fact]
        public void Popover_hidden_until_trigger_clicked()
        {
            var cut = Render<Popover>(p => p
                .AddChildContent("Trigger")
                .Add(x => x.PopoverTemplate, "<em>Details</em>"));

            Assert.Empty(cut.FindAll("span.popover"));
        }

        [Fact]
        public void Clicking_trigger_shows_popover_template()
        {
            var cut = Render<Popover>(p => p
                .AddChildContent("Trigger")
                .Add(x => x.PopoverTemplate, "<em>Details</em>"));

            cut.Find("div.popover-container > span").Click();

            var popover = cut.Find("span.popover");
            Assert.Contains("Details", popover.TextContent);
        }

        [Fact]
        public void Clicking_trigger_twice_hides_popover()
        {
            var cut = Render<Popover>(p => p
                .AddChildContent("Trigger")
                .Add(x => x.PopoverTemplate, "<em>Details</em>"));

            var trigger = cut.Find("div.popover-container > span");
            trigger.Click();
            Assert.Single(cut.FindAll("span.popover"));

            cut.Find("div.popover-container > span").Click();
            Assert.Empty(cut.FindAll("span.popover"));
        }
    }
}
