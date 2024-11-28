using Tabler.Docs.Components.Modals;

namespace TabBlazor.Tests
{
    public class RenderComponentTests
    {
        [Fact]
        public void RenderComponent_can_render_when_setting_cascading_parameters()
        {
            var component = new RenderComponent<TestModalContent>()
                .Set(p => p.CascadingParameter, true);
            Assert.NotNull(component);
        }

        [Fact]
        public void RenderComponent_can_render_when_setting_parameters()
        {
            var component = new RenderComponent<TestModalContent>()
                .Set(p => p.ReportName, "TestReport");
            Assert.NotNull(component);
        }
    }
}