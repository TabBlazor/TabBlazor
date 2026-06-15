namespace TabBlazor.Tests.Components
{
    public class AlertTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_alert_with_title_and_content()
        {
            var cut = Render<Alert>(p => p
                .Add(a => a.Title, "Heads up")
                .AddChildContent("Body text"));

            Assert.Contains("alert", cut.Find("div").ClassList);
            Assert.Equal("Heads up", cut.Find("h4.alert-title").TextContent);
            Assert.Contains("Body text", cut.Markup);
        }

        [Fact]
        public void No_title_element_when_title_empty()
        {
            var cut = Render<Alert>(p => p.AddChildContent("Body"));
            Assert.Empty(cut.FindAll("h4.alert-title"));
        }

        [Fact]
        public void Dismissible_renders_close_button()
        {
            var cut = Render<Alert>(p => p.Add(a => a.Dismissible, true));

            Assert.Contains("alert-dismissible", cut.Find("div").ClassList);
            Assert.Single(cut.FindAll("button.btn-close"));
        }

        [Fact]
        public void Clicking_close_dismisses_alert()
        {
            var cut = Render<Alert>(p => p
                .Add(a => a.Dismissible, true)
                .AddChildContent("Body"));

            cut.Find("button.btn-close").Click();

            Assert.Empty(cut.FindAll("div.alert"));
        }

        [Fact]
        public void Important_adds_important_class()
        {
            var cut = Render<Alert>(p => p.Add(a => a.Important, true));
            Assert.Contains("alert-important", cut.Find("div").ClassList);
        }
    }
}
