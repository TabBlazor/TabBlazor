using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using TabBlazor.Services;

namespace TabBlazor.Tests.Components
{
    public class OffcanvasViewTests : TabBlazorTestContext
    {
        private OverlayLayerService OverlayLayers => Services.GetRequiredService<OverlayLayerService>();

        private IRenderedComponent<OffcanvasView> RenderOffcanvas(OffcanvasOptions options, Action? onClosed = null) =>
            Render<OffcanvasView>(p => p
                .Add(o => o.Title, "Offcanvas title")
                .Add(o => o.Options, options)
                .Add(o => o.OnClosed, onClosed ?? (() => { }))
                .AddChildContent("<span class=\"content\">body</span>"));

        [Fact]
        public void Renders_panel_with_position_class()
        {
            var cut = RenderOffcanvas(new OffcanvasOptions { Position = OffcanvasPosition.End });

            var panel = cut.Find("div.offcanvas");
            Assert.Contains("offcanvas-end", panel.ClassList);
            Assert.Contains("show", panel.ClassList);
        }

        [Fact]
        public void Adds_narrow_floating_and_wrapper_classes_when_set()
        {
            var cut = RenderOffcanvas(new OffcanvasOptions { Narrow = true, Floating = true, WrapperCssClass = "custom" });

            var panel = cut.Find("div.offcanvas");
            Assert.Contains("offcanvas-narrow", panel.ClassList);
            Assert.Contains("offcanvas-floating", panel.ClassList);
            Assert.Contains("custom", panel.ClassList);
        }

        [Fact]
        public void Renders_title_and_child_content()
        {
            var cut = RenderOffcanvas(new OffcanvasOptions());

            var title = cut.Find(".offcanvas-title");
            Assert.Equal("Offcanvas title", title.TextContent);
            Assert.Equal(title.Id, cut.Find("div.offcanvas").GetAttribute("aria-labelledby"));
            Assert.Equal("body", cut.Find(".offcanvas-body span.content").TextContent);
        }

        [Fact]
        public void Renders_backdrop_one_step_below_panel()
        {
            var cut = RenderOffcanvas(new OffcanvasOptions { Backdrop = true });

            Assert.Equal("z-index: 1200", cut.Find("div.offcanvas").GetAttribute("style"));
            Assert.Equal("z-index: 1199", cut.Find("div.offcanvas-backdrop").GetAttribute("style"));
        }

        [Fact]
        public void Omits_backdrop_when_disabled()
        {
            var cut = RenderOffcanvas(new OffcanvasOptions { Backdrop = false });

            Assert.Empty(cut.FindAll("div.offcanvas-backdrop"));
        }

        [Fact]
        public void Stacks_above_overlay_already_open()
        {
            OverlayLayers.Register(new object());

            var cut = RenderOffcanvas(new OffcanvasOptions());

            Assert.Equal("z-index: 1210", cut.Find("div.offcanvas").GetAttribute("style"));
        }

        [Fact]
        public void Invokes_on_closed_when_close_button_clicked()
        {
            var closed = false;
            var cut = RenderOffcanvas(new OffcanvasOptions(), () => closed = true);

            cut.Find("button.btn-close").Click();

            Assert.True(closed);
        }

        [Fact]
        public void Invokes_on_closed_on_escape_when_close_on_esc()
        {
            var closed = false;
            var cut = RenderOffcanvas(new OffcanvasOptions { CloseOnEsc = true }, () => closed = true);

            cut.Find("div.offcanvas").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            Assert.True(closed);
        }

        [Fact]
        public void Ignores_escape_when_close_on_esc_disabled()
        {
            var closed = false;
            var cut = RenderOffcanvas(new OffcanvasOptions { CloseOnEsc = false }, () => closed = true);

            cut.Find("div.offcanvas").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            Assert.False(closed);
        }

        [Fact]
        public void Ignores_escape_when_another_overlay_is_above()
        {
            var closed = false;
            var cut = RenderOffcanvas(new OffcanvasOptions { CloseOnEsc = true }, () => closed = true);
            cut.InvokeAsync(() => OverlayLayers.Register(new object()));

            cut.Find("div.offcanvas").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            Assert.False(closed);
        }

        [Fact]
        public async Task Invokes_on_closed_on_click_outside_when_top_most()
        {
            var closed = false;
            var cut = RenderOffcanvas(new OffcanvasOptions { CloseOnClickOutside = true }, () => closed = true);

            await cut.InvokeAsync(() => cut.FindComponent<ClickOutside>().Instance.InvokeClickOutside());

            Assert.True(closed);
        }

        [Fact]
        public async Task Ignores_click_outside_when_another_overlay_is_above()
        {
            var closed = false;
            var cut = RenderOffcanvas(new OffcanvasOptions { CloseOnClickOutside = true }, () => closed = true);
            await cut.InvokeAsync(() => OverlayLayers.Register(new object()));

            await cut.InvokeAsync(() => cut.FindComponent<ClickOutside>().Instance.InvokeClickOutside());

            Assert.False(closed);
        }

        [Fact]
        public async Task Deactivates_click_outside_while_another_overlay_is_above()
        {
            var cut = RenderOffcanvas(new OffcanvasOptions { CloseOnClickOutside = true });
            var overlayAbove = new object();

            await cut.InvokeAsync(() => OverlayLayers.Register(overlayAbove));
            Assert.False(cut.FindComponent<ClickOutside>().Instance.Active);

            await cut.InvokeAsync(() => OverlayLayers.Unregister(overlayAbove));
            Assert.True(cut.FindComponent<ClickOutside>().Instance.Active);
        }

        [Fact]
        public void Keeps_click_outside_inactive_when_close_on_click_outside_disabled()
        {
            var cut = RenderOffcanvas(new OffcanvasOptions { CloseOnClickOutside = false });

            Assert.False(cut.FindComponent<ClickOutside>().Instance.Active);
        }

        [Fact]
        public void Releases_layer_when_closed_through_container()
        {
            var offcanvasService = Services.GetRequiredService<IOffcanvasService>();
            var cut = Render<OffcanvasContainer>();
            cut.InvokeAsync(() => offcanvasService.ShowAsync("Title", new RenderComponent<Badge>()));
            Assert.Single(cut.FindAll("div.offcanvas"));

            cut.Find("button.btn-close").Click();

            Assert.Empty(cut.FindAll("div.offcanvas"));
            Assert.Equal(1200, OverlayLayers.Register(new object()));
        }

        [Theory]
        [InlineData(OffcanvasPosition.End, OffcanvasSize.Large, "--tblr-offcanvas-width: min(50rem, 100vw)")]
        [InlineData(OffcanvasPosition.Bottom, OffcanvasSize.Small, "--tblr-offcanvas-height: 25vh")]
        [InlineData(OffcanvasPosition.Start, OffcanvasSize.Full, "--tblr-offcanvas-width: 100vw")]
        public void Size_sets_the_panel_width_or_height(OffcanvasPosition position, OffcanvasSize size, string expected)
        {
            var cut = RenderOffcanvas(new OffcanvasOptions { Position = position, Size = size });

            var panel = cut.Find("div.offcanvas");
            Assert.Contains(expected, panel.GetAttribute("style"));
            Assert.Contains($"offcanvas-size-{size.ToString().ToLower()}", panel.ClassList);
        }

        [Fact]
        public void On_closing_can_keep_the_panel_open()
        {
            var closed = false;
            var cut = Render<OffcanvasView>(p => p
                .Add(o => o.Options, new OffcanvasOptions { CloseOnEsc = true })
                .Add(o => o.OnClosing, args => args.Cancel = true)
                .Add(o => o.OnClosed, () => closed = true));

            cut.Find("button.btn-close").Click();
            cut.Find("div.offcanvas").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            Assert.False(closed);
        }

        [Fact]
        public void Options_on_closing_can_keep_the_panel_open()
        {
            var asked = 0;
            var closed = false;
            var cut = RenderOffcanvas(new OffcanvasOptions
            {
                OnClosing = args =>
                {
                    asked++;
                    args.Cancel = asked == 1;
                    return Task.CompletedTask;
                }
            }, () => closed = true);

            cut.Find("button.btn-close").Click();
            Assert.False(closed);

            cut.Find("button.btn-close").Click();
            Assert.True(closed);
        }

        [Fact]
        public void Renders_title_template_and_footer()
        {
            var cut = Render<OffcanvasView>(p => p
                .Add(o => o.Options, new OffcanvasOptions())
                .Add(o => o.TitleTemplate, b => b.AddMarkupContent(0, "<em>Order 7</em>"))
                .Add(o => o.FooterTemplate, b => b.AddMarkupContent(0, "<button class=\"save\">Save</button>")));

            Assert.Equal("Order 7", cut.Find(".offcanvas-title em").TextContent);
            Assert.NotNull(cut.Find(".offcanvas-footer button.save"));
        }

        [Fact]
        public void Hiding_plays_the_closing_animation()
        {
            var cut = Render<OffcanvasView>(p => p
                .Add(o => o.Options, new OffcanvasOptions { Backdrop = true })
                .Add(o => o.Hiding, true));

            Assert.Contains("offcanvas-hiding", cut.Find("div.offcanvas").ClassList);
            Assert.Contains("offcanvas-backdrop-hiding", cut.Find("div.offcanvas-backdrop").ClassList);
        }

        [Fact]
        public void Is_modal_only_with_a_backdrop()
        {
            Assert.Equal("true", RenderOffcanvas(new OffcanvasOptions { Backdrop = true }).Find("div.offcanvas").GetAttribute("aria-modal"));
            Assert.Null(RenderOffcanvas(new OffcanvasOptions { Backdrop = false }).Find("div.offcanvas").GetAttribute("aria-modal"));
        }

        [Fact]
        public async Task Gives_focus_back_when_closed()
        {
            var cut = RenderOffcanvas(new OffcanvasOptions());
            var panelId = cut.Find("div.offcanvas").Id;

            Assert.Equal(panelId, JSInterop.VerifyInvoke("tabBlazor.saveFocus").Arguments[0]);

            await DisposeComponentsAsync();

            Assert.Equal(panelId, JSInterop.VerifyInvoke("tabBlazor.restoreFocus").Arguments[0]);
        }
    }
}
