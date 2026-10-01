using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using TabBlazor.Services;

namespace TabBlazor.Tests.Components
{
    public class ModalViewTests : TabBlazorTestContext
    {
        private OverlayLayerService OverlayLayers => Services.GetRequiredService<OverlayLayerService>();
        private IModalService ModalService => Services.GetRequiredService<IModalService>();

        private IRenderedComponent<ModalContainer> ShowModal(ModalOptions options)
        {
            var cut = Render<ModalContainer>();
            cut.InvokeAsync(() => ModalService.ShowAsync("Title", new RenderComponent<Badge>(), options));
            return cut;
        }

        [Fact]
        public void Renders_backdrop_one_step_below_modal()
        {
            var cut = ShowModal(new ModalOptions { Backdrop = true });

            Assert.Contains("z-index: 1200", cut.Find("div.modal").GetAttribute("style"));
            Assert.Equal("z-index: 1199", cut.Find("div.modal-backdrop").GetAttribute("style"));
        }

        [Fact]
        public void Stacks_above_overlay_already_open()
        {
            OverlayLayers.Register(new object());

            var cut = ShowModal(new ModalOptions());

            Assert.Contains("z-index: 1210", cut.Find("div.modal").GetAttribute("style"));
        }

        [Fact]
        public void Closes_on_click_outside_when_top_most()
        {
            var cut = ShowModal(new ModalOptions { CloseOnClickOutside = true });

            cut.Find("div.modal").Click();

            Assert.Empty(ModalService.Modals);
        }

        [Fact]
        public void Ignores_click_outside_when_another_overlay_is_above()
        {
            var cut = ShowModal(new ModalOptions { CloseOnClickOutside = true });
            OverlayLayers.Register(new object());

            cut.Find("div.modal").Click();

            Assert.Single(ModalService.Modals);
        }

        [Fact]
        public void Ignores_escape_when_another_overlay_is_above()
        {
            var cut = ShowModal(new ModalOptions { CloseOnEsc = true });
            OverlayLayers.Register(new object());

            cut.Find("div.modal").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            Assert.Single(ModalService.Modals);
        }

        [Fact]
        public void Releases_layer_when_closed()
        {
            var cut = ShowModal(new ModalOptions());

            cut.InvokeAsync(() => ModalService.Close());

            Assert.Empty(cut.FindAll("div.modal"));
            Assert.Equal(1200, OverlayLayers.Register(new object()));
        }
    }
}
