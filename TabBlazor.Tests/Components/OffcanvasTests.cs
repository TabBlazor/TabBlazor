using System;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace TabBlazor.Tests.Components
{
    public class OffcanvasTests : TabBlazorTestContext
    {
        private readonly FakeTimeProvider time = new();

        public OffcanvasTests()
        {
            Services.AddSingleton<TimeProvider>(time);
        }

        private IRenderedComponent<Offcanvas> RenderOffcanvas(bool open, Action<ComponentParameterCollectionBuilder<Offcanvas>>? configure = null) =>
            Render<Offcanvas>(p =>
            {
                p.Add(o => o.Open, open).Add(o => o.Title, "Order").AddChildContent("<span class=\"content\">body</span>");
                configure?.Invoke(p);
            });

        [Fact]
        public void Renders_nothing_while_closed()
        {
            var cut = RenderOffcanvas(false);

            Assert.Empty(cut.FindAll("div.offcanvas"));
        }

        [Fact]
        public void Renders_the_panel_with_its_settings_while_open()
        {
            var cut = RenderOffcanvas(true, p => p
                .Add(o => o.Position, OffcanvasPosition.Start)
                .Add(o => o.Size, OffcanvasSize.Large)
                .Add(o => o.Backdrop, false)
                .Add(o => o.Class, "my-panel"));

            var panel = cut.Find("div.offcanvas");
            Assert.Contains("offcanvas-start", panel.ClassList);
            Assert.Contains("offcanvas-size-large", panel.ClassList);
            Assert.Contains("my-panel", panel.ClassList);
            Assert.Equal("Order", cut.Find(".offcanvas-title").TextContent.Trim());
            Assert.NotNull(cut.Find(".offcanvas-body .content"));
            Assert.Empty(cut.FindAll(".offcanvas-backdrop"));
        }

        [Fact]
        public void Content_re_renders_without_closing()
        {
            var cut = RenderOffcanvas(true);
            var panelId = cut.Find("div.offcanvas").Id;

            cut.Render(p => p.AddChildContent("<span class=\"content\">next item</span>"));

            Assert.Equal("next item", cut.Find(".content").TextContent);
            Assert.Equal(panelId, cut.Find("div.offcanvas").Id);
        }

        [Fact]
        public void Close_button_closes_with_an_animation_then_removes_the_panel()
        {
            bool? open = null;
            var closed = false;
            var cut = RenderOffcanvas(true, p => p
                .Add(o => o.OpenChanged, v => open = v)
                .Add(o => o.OnClosed, () => closed = true));

            cut.Find("button.btn-close").Click();

            Assert.False(open);
            Assert.Contains("offcanvas-hiding", cut.Find("div.offcanvas").ClassList);
            Assert.False(closed);

            time.Advance(Offcanvas.HideDuration);

            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("div.offcanvas")));
            Assert.True(closed);
        }

        [Fact]
        public void Esc_closes_by_default()
        {
            bool? open = null;
            var cut = RenderOffcanvas(true, p => p.Add(o => o.OpenChanged, v => open = v));

            cut.Find("div.offcanvas").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            Assert.False(open);
        }

        [Fact]
        public void On_closing_can_keep_it_open()
        {
            bool? open = null;
            var cut = RenderOffcanvas(true, p => p
                .Add(o => o.OpenChanged, v => open = v)
                .Add(o => o.OnClosing, args => args.Cancel = true));

            cut.Find("button.btn-close").Click();
            cut.InvokeAsync(() => cut.Instance.CloseAsync());

            Assert.Null(open);
            Assert.DoesNotContain("offcanvas-hiding", cut.Find("div.offcanvas").ClassList);
        }

        [Fact]
        public void Setting_open_to_false_hides_the_panel()
        {
            var cut = RenderOffcanvas(true);

            cut.Render(p => p.Add(o => o.Open, false));
            Assert.Contains("offcanvas-hiding", cut.Find("div.offcanvas").ClassList);

            time.Advance(Offcanvas.HideDuration);
            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("div.offcanvas")));
        }

        [Fact]
        public void Opening_again_while_hiding_keeps_the_panel()
        {
            var cut = RenderOffcanvas(true);
            cut.Render(p => p.Add(o => o.Open, false));

            cut.Render(p => p.Add(o => o.Open, true));
            time.Advance(Offcanvas.HideDuration);

            Assert.DoesNotContain("offcanvas-hiding", cut.Find("div.offcanvas").ClassList);
            Assert.Single(cut.FindAll("div.offcanvas"));
        }
    }
}
