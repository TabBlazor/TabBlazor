using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;

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
            Action? onPreload = null, Action? onActivated = null, string? id = null) => builder =>
        {
            builder.OpenComponent<Tab>(0);
            builder.SetKey(name);
            builder.AddAttribute(1, nameof(Tab.Title), name);
            builder.AddAttribute(2, nameof(Tab.Active), active);
            if (id != null)
            {
                builder.AddAttribute(7, nameof(Tab.Id), id);
            }
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

        private IRenderedComponent<Tabs> RenderTabs(TabPreload? preload = null, bool? keepAlive = null,
            int? preloadDelay = null, string? urlParameter = null, TabUrlHistory? urlHistory = null,
            params RenderFragment[] items) =>
            Render<Tabs>(p => p
                .Add(t => t.Preload, preload)
                .Add(t => t.KeepAlive, keepAlive)
                .Add(t => t.PreloadDelay, preloadDelay)
                .Add(t => t.UrlParameter, urlParameter)
                .Add(t => t.UrlHistory, urlHistory)
                .AddChildContent(b =>
                {
                    foreach (var item in items)
                    {
                        item(b);
                    }
                }));

        private static AngleSharp.Dom.IElement Header(IRenderedComponent<Tabs> cut, int index) =>
            cut.FindAll("a.nav-link")[index];

        private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

        private IRenderedComponent<Tabs> RenderLinkedTabs(string url, TabUrlHistory? urlHistory = null,
            params RenderFragment[] items)
        {
            Navigation.NavigateTo(url);
            return RenderTabs(urlParameter: "tab", urlHistory: urlHistory, items: items);
        }

        private static string ActiveContent(IRenderedComponent<Tabs> cut) =>
            cut.Find("div.tab-pane.active span").ClassName!;

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
        public void Selects_tab_by_id_from_url()
        {
            var cut = RenderLinkedTabs("/?tab=orders", items: [TabItem("one"), TabItem("two", id: "orders")]);

            Assert.Equal("content-two", ActiveContent(cut));
        }

        [Fact]
        public void Selects_tab_by_position_from_url()
        {
            var cut = RenderLinkedTabs("/?tab=3", items: [TabItem("one"), TabItem("two"), TabItem("three")]);

            Assert.Equal("content-three", ActiveContent(cut));
        }

        [Fact]
        public void Id_match_wins_over_position()
        {
            var cut = RenderLinkedTabs("/?tab=2", items: [TabItem("one"), TabItem("two"), TabItem("three", id: "2")]);

            Assert.Equal("content-three", ActiveContent(cut));
        }

        [Fact]
        public void Url_selection_wins_over_active_parameter()
        {
            var cut = RenderLinkedTabs("/?tab=1", items: [TabItem("one"), TabItem("two", active: true)]);

            Assert.Equal("content-one", ActiveContent(cut));
        }

        [Theory]
        [InlineData("/?tab=missing")]
        [InlineData("/?tab=9")]
        [InlineData("/?tab=0")]
        public void Unknown_url_value_falls_back_to_default_tab(string url)
        {
            var cut = RenderLinkedTabs(url, items: [TabItem("one"), TabItem("two", active: true)]);

            Assert.Equal("content-two", ActiveContent(cut));
        }

        [Fact]
        public void Ignores_url_when_url_parameter_is_not_set()
        {
            Navigation.NavigateTo("/?tab=2");

            var cut = RenderTabs(items: [TabItem("one"), TabItem("two")]);

            Assert.Equal("content-one", ActiveContent(cut));
            Assert.Null(Header(cut, 1).GetAttribute("href"));
        }

        [Fact]
        public void Invokes_on_activated_for_tab_selected_from_url()
        {
            var activated = new List<string>();
            RenderLinkedTabs("/?tab=2", items:
            [
                TabItem("one", onActivated: () => activated.Add("one")),
                TabItem("two", onActivated: () => activated.Add("two"))
            ]);

            Assert.Equal(["two"], activated);
        }

        [Fact]
        public void Renders_tab_urls_as_header_links()
        {
            var cut = RenderLinkedTabs("/page?filter=open", items: [TabItem("one"), TabItem("two", id: "orders"), TabItem("three")]);

            Assert.Equal("http://localhost/page?filter=open", Header(cut, 0).GetAttribute("href"));
            Assert.Equal("http://localhost/page?filter=open&tab=orders", Header(cut, 1).GetAttribute("href"));
            Assert.Equal("http://localhost/page?filter=open&tab=3", Header(cut, 2).GetAttribute("href"));
            Assert.Null(Header(cut, 1).GetAttribute("tabindex"));
        }

        [Fact]
        public void Encodes_and_decodes_tab_id_in_url()
        {
            var cut = RenderLinkedTabs("/page?tab=sales%20%26%20orders", items: [TabItem("one"), TabItem("two", id: "sales & orders")]);

            Assert.Equal("content-two", ActiveContent(cut));
            Assert.Equal("http://localhost/page?tab=sales%20%26%20orders", Header(cut, 1).GetAttribute("href"));
        }

        [Fact]
        public void Keeps_fragment_when_writing_url()
        {
            var cut = RenderLinkedTabs("/page?filter=open#details", items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).Click();

            Assert.Equal("http://localhost/page?filter=open&tab=2#details", Navigation.Uri);
        }

        [Fact]
        public void Repeated_url_parameter_uses_first_value_and_is_collapsed_on_click()
        {
            var cut = RenderLinkedTabs("/page?tab=2&tab=3", items: [TabItem("one"), TabItem("two"), TabItem("three")]);

            Assert.Equal("content-two", ActiveContent(cut));

            Header(cut, 2).Click();

            Assert.Equal("http://localhost/page?tab=3", Navigation.Uri);
        }

        [Fact]
        public void Clicking_tab_replaces_url()
        {
            var cut = RenderLinkedTabs("/page", items: [TabItem("one"), TabItem("two", id: "orders")]);

            Header(cut, 1).Click();

            Assert.Equal("http://localhost/page?tab=orders", Navigation.Uri);
            Assert.True(Navigation.History.First().Options.ReplaceHistoryEntry);
            Assert.Equal("content-two", ActiveContent(cut));
        }

        [Fact]
        public void Clicking_default_tab_removes_url_parameter()
        {
            var cut = RenderLinkedTabs("/page?tab=2&filter=open", items: [TabItem("one"), TabItem("two")]);

            Header(cut, 0).Click();

            Assert.Equal("http://localhost/page?filter=open", Navigation.Uri);
            Assert.Equal("content-one", ActiveContent(cut));
        }

        [Fact]
        public void Clicking_tab_does_not_navigate_when_history_is_push()
        {
            var cut = RenderLinkedTabs("/page", TabUrlHistory.Push, items: [TabItem("one"), TabItem("two")]);
            var historyCount = Navigation.History.Count;

            Header(cut, 1).Click();

            Assert.Equal(historyCount, Navigation.History.Count);
            Assert.Equal("content-two", ActiveContent(cut));
            Assert.Equal("http://localhost/page?tab=2", Header(cut, 1).GetAttribute("href"));
        }

        [Fact]
        public void Url_change_selects_matching_tab()
        {
            var cut = RenderLinkedTabs("/page", items: [TabItem("one"), TabItem("two"), TabItem("three", id: "orders")]);

            Navigation.NavigateTo("/page?tab=orders");

            cut.WaitForAssertion(() => Assert.Equal("content-three", ActiveContent(cut)));
        }

        [Fact]
        public void Removing_url_parameter_selects_default_tab()
        {
            var cut = RenderLinkedTabs("/page?tab=2", items: [TabItem("one"), TabItem("two")]);

            Navigation.NavigateTo("/page");

            cut.WaitForAssertion(() => Assert.Equal("content-one", ActiveContent(cut)));
        }

        [Fact]
        public void Url_change_keeps_other_tabs_url_parameter()
        {
            Navigation.NavigateTo("/page?inner=2");
            var outer = RenderTabs(urlParameter: "outer", items: [TabItem("one"), TabItem("two")]);
            var inner = Render<Tabs>(p => p
                .Add(t => t.UrlParameter, "inner")
                .AddChildContent(b => { TabItem("a")(b); TabItem("b")(b); }));

            Header(outer, 1).Click();

            Assert.Equal("http://localhost/page?inner=2&outer=2", Navigation.Uri);
            Assert.Contains("active", inner.FindAll("a.nav-link")[1].ClassList);
        }

        [Fact]
        public void Two_tabs_with_different_url_parameters_stay_independent()
        {
            Navigation.NavigateTo("/page");
            var orders = RenderTabs(urlParameter: "orders", items: [TabItem("one"), TabItem("two"), TabItem("three")]);
            var settings = Render<Tabs>(p => p
                .Add(t => t.UrlParameter, "settings")
                .AddChildContent(b => { TabItem("a", id: "general")(b); TabItem("b", id: "security")(b); }));

            Header(orders, 2).Click();
            settings.FindAll("a.nav-link")[1].Click();

            Assert.Equal("http://localhost/page?orders=3&settings=security", Navigation.Uri);
            Assert.Contains("active", Header(orders, 2).ClassList);
            Assert.Contains("active", settings.FindAll("a.nav-link")[1].ClassList);
            Assert.Equal("http://localhost/page?orders=2&settings=security", Header(orders, 1).GetAttribute("href"));

            Navigation.NavigateTo("/page?orders=2&settings=security");

            orders.WaitForAssertion(() => Assert.Contains("active", Header(orders, 1).ClassList));
            Assert.Contains("active", settings.FindAll("a.nav-link")[1].ClassList);
        }

        private sealed class PageSwitcher : ComponentBase
        {
            [Parameter] public string Page { get; set; } = "first";

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<Tabs>(0);
                builder.SetKey(Page);
                builder.AddAttribute(1, nameof(Tabs.UrlParameter), "tab");
                builder.AddAttribute(2, nameof(Tabs.ChildContent), (RenderFragment)(content =>
                {
                    content.OpenComponent<Tab>(0);
                    content.AddAttribute(1, nameof(Tab.Title), Page);
                    content.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }

        [Fact]
        public void Throws_when_two_tabs_share_url_parameter()
        {
            RenderTabs(urlParameter: "tab", items: [TabItem("one")]);

            var exception = Assert.ThrowsAny<InvalidOperationException>(() =>
                RenderTabs(urlParameter: "TAB", items: [TabItem("two")]));
            Assert.Contains("UrlParameter 'TAB'", exception.Message);
        }

        [Fact]
        public async Task Allows_url_parameter_again_after_previous_tabs_is_disposed()
        {
            RenderTabs(urlParameter: "tab", items: [TabItem("one")]);
            await DisposeComponentsAsync();

            RenderTabs(urlParameter: "tab", items: [TabItem("two")]);
        }

        [Fact]
        public void Allows_replacing_tabs_with_same_url_parameter_in_one_render()
        {
            var cut = Render<PageSwitcher>();

            cut.Render(p => p.Add(s => s.Page, "second"));

            Assert.Equal("second", cut.Find("a.nav-link").TextContent.Trim());
        }

        [Fact]
        public void Binds_lowercase_id_attribute()
        {
            Navigation.NavigateTo("/?tab=orders");

            var tabs = Render<Tabs>(p => p
                .Add(t => t.UrlParameter, "tab")
                .AddChildContent(b =>
                {
                    TabItem("one")(b);
                    b.OpenComponent<Tab>(10);
                    b.AddMultipleAttributes(11, new Dictionary<string, object> { ["id"] = "orders", ["Title"] = "two" });
                    b.CloseComponent();
                }));

            Assert.Contains("active", tabs.FindAll("a.nav-link")[1].ClassList);
        }

        [Fact]
        public void Throws_on_duplicate_tab_id()
        {
            Assert.ThrowsAny<InvalidOperationException>(() =>
                RenderTabs(items: [TabItem("one", id: "same"), TabItem("two", id: "same")]));
        }

        [Fact]
        public async Task Stops_following_url_after_dispose()
        {
            RenderLinkedTabs("/page", items: [TabItem("one"), TabItem("two")]);
            await DisposeComponentsAsync();

            Navigation.NavigateTo("/page?tab=2");
        }

        private void ConfigureTabsDefaults(Action<TabsOptions> configure) =>
            Services.Configure<TablerOptions>(options => configure(options.Tabs));

        [Fact]
        public void Global_preload_wires_hover_preload()
        {
            ConfigureTabsDefaults(o => o.Preload = TabPreload.Hover);
            var cut = RenderTabs(items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).TriggerEvent("onmouseenter", new MouseEventArgs());

            Assert.Equal(2, cut.FindAll("div.tab-pane").Count);
        }

        [Fact]
        public void Tabs_preload_overrides_global_preload()
        {
            ConfigureTabsDefaults(o => o.Preload = TabPreload.Hover);
            var cut = RenderTabs(TabPreload.None, items: [TabItem("one"), TabItem("two")]);

            Assert.Throws<MissingEventHandlerException>(() =>
                Header(cut, 1).TriggerEvent("onmouseenter", new MouseEventArgs()));
        }

        [Fact]
        public void Tab_preload_overrides_global_preload()
        {
            ConfigureTabsDefaults(o => o.Preload = TabPreload.Eager);
            var cut = RenderTabs(items: [TabItem("one"), TabItem("two", preload: TabPreload.None)]);

            Assert.Single(cut.FindAll("div.tab-pane"));
        }

        [Fact]
        public void Global_preload_delay_postpones_hover_preload()
        {
            ConfigureTabsDefaults(o => { o.Preload = TabPreload.Hover; o.PreloadDelay = 10_000; });
            var cut = RenderTabs(items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).TriggerEvent("onmouseenter", new MouseEventArgs());

            Assert.Single(cut.FindAll("div.tab-pane"));
        }

        [Fact]
        public void Global_keep_alive_keeps_visited_tabs()
        {
            ConfigureTabsDefaults(o => o.KeepAlive = true);
            var cut = RenderTabs(items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).Click();

            Assert.Equal(2, cut.FindAll("div.tab-pane").Count);
        }

        [Fact]
        public void Tabs_keep_alive_overrides_global_keep_alive()
        {
            ConfigureTabsDefaults(o => o.KeepAlive = true);
            var cut = RenderTabs(keepAlive: false, items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).Click();

            Assert.Single(cut.FindAll("div.tab-pane"));
        }

        [Fact]
        public void Global_url_history_push_leaves_navigation_to_link()
        {
            ConfigureTabsDefaults(o => o.UrlHistory = TabUrlHistory.Push);
            var cut = RenderLinkedTabs("/page", items: [TabItem("one"), TabItem("two")]);
            var historyCount = Navigation.History.Count;

            Header(cut, 1).Click();

            Assert.Equal(historyCount, Navigation.History.Count);
        }

        [Fact]
        public void Tabs_url_history_overrides_global_url_history()
        {
            ConfigureTabsDefaults(o => o.UrlHistory = TabUrlHistory.Push);
            var cut = RenderLinkedTabs("/page", TabUrlHistory.Replace, items: [TabItem("one"), TabItem("two")]);

            Header(cut, 1).Click();

            Assert.Equal("http://localhost/page?tab=2", Navigation.Uri);
            Assert.True(Navigation.History.First().Options.ReplaceHistoryEntry);
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
