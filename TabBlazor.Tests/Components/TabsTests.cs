namespace TabBlazor.Tests.Components
{
    public class TabsTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_card_with_header_tabs()
        {
            var cut = Render<Tabs>();

            Assert.Contains("card", cut.Find("div").ClassList);
            Assert.Single(cut.FindAll("ul.nav-tabs.card-header-tabs"));
        }

        [Fact]
        public void Renders_tab_content_container()
        {
            var cut = Render<Tabs>();

            Assert.Single(cut.FindAll("div.tab-content"));
            Assert.Single(cut.FindAll("div.tab-pane.active.show"));
        }

        [Fact]
        public void Renders_child_content_into_tab_header()
        {
            var cut = Render<Tabs>(p => p.AddChildContent("<li class=\"injected\"></li>"));

            Assert.Single(cut.FindAll("ul.card-header-tabs li.injected"));
        }

        [Fact]
        public void Has_no_active_tab_by_default()
        {
            var cut = Render<Tabs>();

            Assert.Null(cut.Instance.ActiveTab);
        }
    }
}
