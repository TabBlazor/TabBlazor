using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace TabBlazor.Tests.Components
{
    public class TabsTests : TabBlazorTestContext
    {
        private readonly List<string> initializedContent = new();

        private sealed class TabContent : ComponentBase
        {
            [Parameter] public string Name { get; set; } = "";
            [Parameter] public List<string> InitializedContent { get; set; } = new();
            [CascadingParameter(Name = "IsActiveTab")] public bool IsActiveTab { get; set; }

            protected override void OnInitialized() => InitializedContent.Add(Name);

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", $"content-{Name}");
                builder.AddAttribute(2, "data-active", IsActiveTab ? "true" : "false");
                builder.CloseElement();
            }
        }

        private RenderFragment TabItem(string name, TabPreload? preload = null, bool active = false,
            Action? onPreload = null, Action? onActivated = null) => builder =>
        {
            builder.OpenComponent<Tab>(0);
            builder.SetKey(name);
            builder.AddAttribute(1, nameof(Tab.Title), name);
            builder.AddAttribute(2, nameof(Tab.Active), active);
            if (preload != null)
            {
                builder.AddAttribute(3, nameof(Tab.Preload), preload);
            }
            if (onPreload != null)
            {
                builder.AddAttribute(4, nameof(Tab.OnPreload), EventCallback.Factory.Create(this, onPreload));
            }
            if (onActivated != null)
            {
                builder.AddAttribute(5, nameof(Tab.OnActivated), EventCallback.Factory.Create(this, onActivated));
            }
            builder.AddAttribute(6, nameof(Tab.ChildContent), (RenderFragment)(content =>
            {
                content.OpenComponent<TabContent>(0);
                content.AddAttribute(1, nameof(TabContent.Name), name);
                content.AddAttribute(2, nameof(TabContent.InitializedContent), initializedContent);
                content.CloseComponent();
            }));
            builder.CloseComponent();
        };

        private IRenderedComponent<Tabs> RenderTabs(TabPreload preload = TabPreload.None, bool keepAlive = false,
            int preloadDelay = 0, params RenderFragment[] items) =>
            Render<Tabs>(p => p
                .Add(t => t.Preload, preload)
                .Add(t => t.KeepAlive, keepAlive)
                .Add(t => t.PreloadDelay, preloadDelay)
                .AddChildContent(b =>
                {
                    foreach (var item in items)
                    {
                        item(b);
                    }
                }));

        private static AngleSharp.Dom.IElement Header(IRenderedComponent<Tabs> cut, int index) =>
            cut.FindAll("a.nav-link")[index];

        [Fact]
        public void Renders_card_with_header_tabs()
        {
            var cut = Render<Tabs>();

            Assert.Contains("card", cut.Find("div").ClassList);
            Assert.Single(cut.FindAll("ul.nav-tabs.card-header-tabs"));
        }

        [Fact]
        public void Renders_empty_tab_content_container_without_tabs()
        {
            var cut = Render<Tabs>();

            Assert.Single(cut.FindAll("div.tab-content"));
            Assert.Empty(cut.FindAll("div.tab-pane"));
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

        [Fact]
        public void Renders_only_first_tab_pane_by_default()
        {
            var cut = RenderTabs(items: [TabItem("one"), TabItem("two"), TabItem("three")]);

            var pane = Assert.Single(cut.FindAll("div.tab-pane"));
            Assert.Contains("active", pane.ClassList);
            Assert.NotNull(pane.QuerySelector(".content-one"));
            Assert.Equal(["one"], initializedContent);
        }

        [Fact]
        public void Renders_tab_marked_active_instead_of_first()
        {
            var cut = RenderTabs(items: [TabItem("one"), TabItem("two", active: true)]);

            var pane = Assert.Single(cut.FindAll("div.tab-pane"));
            Assert.NotNull(pane.QuerySelector(".content-two"));
        }

        [Fact]
        public void Clicking_tab_replaces_content_when_not_kept_alive()
        {
            var cut = RenderTabs(items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).Click();

            var pane = Assert.Single(cut.FindAll("div.tab-pane"));
            Assert.NotNull(pane.QuerySelector(".content-two"));
            Assert.Contains("active", Header(cut, 1).ClassList);
        }

        [Fact]
        public void Does_not_wire_hover_handlers_when_preload_is_none()
        {
            var cut = RenderTabs(items: [TabItem("one"), TabItem("two")]);

            Assert.Throws<MissingEventHandlerException>(() =>
                Header(cut, 1).TriggerEvent("onmouseenter", new MouseEventArgs()));
        }

        [Fact]
        public void Hovering_tab_renders_its_content_hidden()
        {
            var cut = RenderTabs(TabPreload.Hover, items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).TriggerEvent("onmouseenter", new MouseEventArgs());

            var panes = cut.FindAll("div.tab-pane");
            Assert.Equal(2, panes.Count);
            Assert.NotNull(panes[1].QuerySelector(".content-two"));
            Assert.DoesNotContain("active", panes[1].ClassList);
        }

        [Fact]
        public void Clicking_preloaded_tab_reuses_its_content()
        {
            var cut = RenderTabs(TabPreload.Hover, items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).TriggerEvent("onmouseenter", new MouseEventArgs());
            Header(cut, 1).Click();

            Assert.Equal(["one", "two"], initializedContent);
            var pane = Assert.Single(cut.FindAll("div.tab-pane"));
            Assert.Contains("active", pane.ClassList);
            Assert.NotNull(pane.QuerySelector(".content-two"));
        }

        [Fact]
        public void Leaving_tab_before_delay_cancels_preload()
        {
            var cut = RenderTabs(TabPreload.Hover, preloadDelay: 50, items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).TriggerEvent("onmouseenter", new MouseEventArgs());
            Header(cut, 1).TriggerEvent("onmouseleave", new MouseEventArgs());
            Thread.Sleep(150);

            Assert.Single(cut.FindAll("div.tab-pane"));
        }

        [Fact]
        public void Hovering_tab_past_delay_preloads_it()
        {
            var cut = RenderTabs(TabPreload.Hover, preloadDelay: 20, items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).TriggerEvent("onmouseenter", new MouseEventArgs());

            cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("div.tab-pane").Count));
        }

        [Fact]
        public void Focusing_tab_preloads_it()
        {
            var cut = RenderTabs(TabPreload.Hover, preloadDelay: 10_000, items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).Focus();

            Assert.Equal(2, cut.FindAll("div.tab-pane").Count);
        }

        [Fact]
        public void Pressing_tab_preloads_it()
        {
            var cut = RenderTabs(TabPreload.Hover, preloadDelay: 10_000, items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).TriggerEvent("onpointerdown", new PointerEventArgs());

            Assert.Equal(2, cut.FindAll("div.tab-pane").Count);
        }

        [Fact]
        public void Invokes_on_preload_once_per_preload()
        {
            var preloadCount = 0;
            var cut = RenderTabs(TabPreload.Hover, items: [TabItem("one"), TabItem("two", onPreload: () => preloadCount++)]);

            Header(cut, 1).TriggerEvent("onmouseenter", new MouseEventArgs());
            Header(cut, 1).TriggerEvent("onmouseleave", new MouseEventArgs());
            Header(cut, 1).TriggerEvent("onmouseenter", new MouseEventArgs());

            Assert.Equal(1, preloadCount);
        }

        [Fact]
        public void Tab_preload_overrides_tabs_preload()
        {
            var cut = RenderTabs(TabPreload.Hover, items: [TabItem("one"), TabItem("two", preload: TabPreload.None)]);

            Assert.Throws<MissingEventHandlerException>(() =>
                Header(cut, 1).TriggerEvent("onmouseenter", new MouseEventArgs()));
        }

        [Fact]
        public void Keeps_visited_tab_rendered_when_kept_alive()
        {
            var cut = RenderTabs(keepAlive: true, items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).Click();
            Header(cut, 0).Click();

            Assert.Equal(2, cut.FindAll("div.tab-pane").Count);
            Assert.Equal(["one", "two"], initializedContent);
        }

        [Fact]
        public void Eager_renders_every_tab_on_first_render()
        {
            var preloadCount = 0;
            var cut = RenderTabs(TabPreload.Eager,
                items: [TabItem("one", onPreload: () => preloadCount++), TabItem("two", onPreload: () => preloadCount++)]);

            var panes = cut.FindAll("div.tab-pane");
            Assert.Equal(2, panes.Count);
            Assert.Contains("active", panes[0].ClassList);
            Assert.DoesNotContain("active", panes[1].ClassList);
            Assert.Equal(2, preloadCount);
        }

        [Fact]
        public void Eager_invokes_on_preload_for_first_tab_when_later_tab_is_active()
        {
            var preloaded = new List<string>();
            var cut = RenderTabs(TabPreload.Eager, items:
            [
                TabItem("one", onPreload: () => preloaded.Add("one")),
                TabItem("two", active: true, onPreload: () => preloaded.Add("two"))
            ]);

            Assert.Equal(["one", "two"], preloaded);
            var panes = cut.FindAll("div.tab-pane");
            Assert.Equal(2, panes.Count);
            Assert.DoesNotContain("active", panes[0].ClassList);
            Assert.Contains("active", panes[1].ClassList);
        }

        [Fact]
        public void Tab_headers_are_keyboard_focusable()
        {
            var cut = RenderTabs(items: [TabItem("one"), TabItem("two")]);

            Assert.All(cut.FindAll("a.nav-link"), header => Assert.Equal("0", header.GetAttribute("tabindex")));
        }

        [Fact]
        public void Pressing_enter_on_tab_header_activates_it()
        {
            var cut = RenderTabs(items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.Contains("active", Header(cut, 1).ClassList);
            Assert.NotNull(cut.Find("div.tab-pane.active .content-two"));
        }

        [Fact]
        public void Pressing_other_key_on_tab_header_does_not_activate_it()
        {
            var cut = RenderTabs(items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).KeyDown(new KeyboardEventArgs { Key = "a" });

            Assert.Contains("active", Header(cut, 0).ClassList);
        }

        [Fact]
        public void Eager_keeps_tabs_rendered_after_switching()
        {
            var cut = RenderTabs(TabPreload.Eager, items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).Click();
            Header(cut, 0).Click();

            Assert.Equal(2, cut.FindAll("div.tab-pane").Count);
            Assert.Equal(["one", "two"], initializedContent);
        }

        [Fact]
        public void Invokes_on_activated_for_initial_tab_and_on_switch_but_not_on_preload()
        {
            var activated = new List<string>();
            var cut = RenderTabs(TabPreload.Hover, items:
            [
                TabItem("one", onActivated: () => activated.Add("one")),
                TabItem("two", onActivated: () => activated.Add("two"))
            ]);

            Header(cut, 1).TriggerEvent("onmouseenter", new MouseEventArgs());
            Assert.Equal(["one"], activated);

            Header(cut, 1).Click();
            Assert.Equal(["one", "two"], activated);
        }

        [Fact]
        public void Cascades_is_active_tab_to_content()
        {
            var cut = RenderTabs(TabPreload.Hover, items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).TriggerEvent("onmouseenter", new MouseEventArgs());
            Assert.Equal("true", cut.Find(".content-one").GetAttribute("data-active"));
            Assert.Equal("false", cut.Find(".content-two").GetAttribute("data-active"));

            Header(cut, 1).Click();
            Assert.Equal("true", cut.Find(".content-two").GetAttribute("data-active"));
        }

        [Fact]
        public async Task Disposing_tabs_with_rendered_panes_does_not_throw()
        {
            RenderTabs(TabPreload.Eager, preloadDelay: 50, items: [TabItem("one"), TabItem("two")]);

            await DisposeComponentsAsync();
        }

        [Fact]
        public void Removing_inactive_tab_keeps_active_tab()
        {
            var cut = RenderTabs(TabPreload.Eager, items: [TabItem("one"), TabItem("two")]);

            cut.Render(p => p.AddChildContent(TabItem("one")));

            var pane = Assert.Single(cut.FindAll("div.tab-pane"));
            Assert.NotNull(pane.QuerySelector(".content-one"));
            Assert.Contains("active", pane.ClassList);
        }

        [Fact]
        public void Removing_active_tab_activates_first_remaining_tab()
        {
            var cut = RenderTabs(items: [TabItem("one"), TabItem("two")]);

            cut.Render(p => p.AddChildContent(TabItem("two")));

            var pane = Assert.Single(cut.FindAll("div.tab-pane"));
            Assert.Contains("active", pane.ClassList);
            Assert.NotNull(pane.QuerySelector(".content-two"));
            Assert.Contains("active", Header(cut, 0).ClassList);
        }

        [Fact]
        public void Removing_last_tab_clears_active_tab()
        {
            var cut = RenderTabs(items: [TabItem("one")]);

            cut.Render(p => p.AddChildContent(_ => { }));

            Assert.Null(cut.Instance.ActiveTab);
            Assert.Empty(cut.FindAll("div.tab-pane"));
        }
    }
}
