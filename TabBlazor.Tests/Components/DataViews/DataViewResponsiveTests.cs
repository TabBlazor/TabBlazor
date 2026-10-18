#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace TabBlazor.Tests.Components.DataViews
{
    public class DataViewResponsiveTests : TabBlazorTestContext
    {
        private void SimulateSmallScreen() =>
            JSInterop.Setup<bool>("tabBlazor.watchMedia", _ => true).SetResult(true);

        private IRenderedComponent<DataView<Row>> RenderView(Action<ComponentParameterCollectionBuilder<DataView<Row>>>? configure = null) =>
            Render<DataView<Row>>(p =>
            {
                p.Add(x => x.Items, Row.Many(30))
                    .Add(x => x.PageSize, 10)
                    .Add(x => x.SaveItem, ctx => Task.FromResult(EditResult.Saved(ctx.Draft)))
                    .Add(x => x.DeleteItem, _ => Task.FromResult(EditResult.Deleted()))
                    .Add(x => x.NewItem, () => new Row())
                    .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Searchable, true))
                    .AddChildContent<DataColumn<Row, decimal>>(c => c.Add(x => x.Value, r => r.Total).Add(x => x.HideBelow, "lg"));
                configure?.Invoke(p);
            });

        [Fact]
        public void Watches_the_mobile_breakpoint()
        {
            RenderView(p => p.Add(x => x.MobileBreakpoint, "lg"));

            var call = JSInterop.VerifyInvoke("tabBlazor.watchMedia");
            Assert.Equal("(max-width: 991.98px)", call.Arguments[1]);
        }

        [Fact]
        public void Card_carries_the_mobile_layout_class()
        {
            Assert.Contains("dv-stacked-md", RenderView().Find(".dataview").ClassList);
            Assert.Contains("dv-scroll-sm", RenderView(p => p
                .Add(x => x.MobileLayout, DataMobileLayout.Scroll)
                .Add(x => x.MobileBreakpoint, "sm")).Find(".dataview").ClassList);
        }

        [Fact]
        public void Hide_below_hides_the_column_on_narrow_screens()
        {
            var cut = RenderView();

            Assert.Contains("d-lg-table-cell", cut.FindAll("thead th")[1].ClassList);
            Assert.Contains("d-none", cut.Find("tbody tr").QuerySelectorAll("td")[1].ClassList);
        }

        [Fact]
        public void Mobile_template_shows_your_cards_below_the_breakpoint()
        {
            var cut = RenderView(p => p.AddChildContent<MobileTemplate<Row>>(t => t
                .Add(x => x.ChildContent, r => b => b.AddMarkupContent(0, $"<span class=\"card-line\">{r.Name}</span>"))));

            Assert.Contains("d-md-none", cut.Find(".dv-mobile-list").ClassList);
            Assert.Equal(10, cut.FindAll(".dv-mobile-list .card-line").Count);
            Assert.Contains("d-md-block", cut.Find(".dv-scroll").ParentElement!.ClassList);
        }

        [Fact]
        public void Small_screens_edit_in_a_full_screen_popup()
        {
            SimulateSmallScreen();
            var cut = RenderView();

            cut.Find("tbody .dv-action-menu button").Click();
            cut.FindAll("tbody .dropdown-item")[0].Click();

            Assert.Empty(cut.FindAll("tr.dv-edit-row"));
            Assert.Contains("modal-fullscreen", cut.Find(".modal-dialog").ClassList);
        }

        [Fact]
        public void Small_screens_put_row_actions_in_the_menu()
        {
            SimulateSmallScreen();
            var cut = RenderView();

            Assert.Contains("dv-small", cut.Find(".dataview").ClassList);
            Assert.Empty(cut.FindAll("tbody button.dv-action"));
            Assert.NotEmpty(cut.FindAll("tbody .dv-action-menu"));
        }

        [Fact]
        public void Small_screens_move_add_refresh_and_page_size_into_the_menu()
        {
            SimulateSmallScreen();
            var cut = RenderView(p => p.Add(x => x.ShowRefresh, true));

            Assert.Empty(cut.FindAll(".dv-toolbar > button.dv-add"));
            Assert.Empty(cut.FindAll(".dv-toolbar > button.dv-refresh"));
            Assert.Empty(cut.FindAll(".dv-page-size"));

            cut.Find("button.dv-menu").Click();
            Assert.NotNull(cut.Find(".dv-menu-add"));
            Assert.NotNull(cut.Find(".dv-menu-refresh"));
            cut.FindAll(".dv-menu-page-size")[0].Click();
            Assert.Equal(10, cut.Instance.CurrentState.PageSize);
        }

        [Fact]
        public void Small_screens_get_a_compact_pager()
        {
            SimulateSmallScreen();
            var cut = RenderView();

            Assert.Equal("1 / 3", cut.Find(".dv-page-compact").TextContent.Trim());
        }

        [Fact]
        public void Side_panel_becomes_a_full_screen_drawer_on_small_screens()
        {
            SimulateSmallScreen();
            var cut = RenderView(p => p
                .Add(x => x.DetailsMode, DataDetailsMode.SidePanel)
                .AddChildContent<DetailsTemplate<Row>>(t => t.Add(x => x.ChildContent, ctx => b => b.AddContent(0, ctx.Item.Name))));

            Assert.Empty(cut.FindAll(".dv-side-panel"));
            cut.FindAll("tbody tr")[0].Click();

            Assert.Contains("offcanvas-size-full", cut.Find(".offcanvas").ClassList);
        }

        [Fact]
        public async Task Crossing_the_breakpoint_switches_layouts()
        {
            var cut = RenderView();
            Assert.DoesNotContain("dv-small", cut.Find(".dataview").ClassList);

            await cut.InvokeAsync(() => cut.Instance.OnMediaChanged(true));

            Assert.Contains("dv-small", cut.Find(".dataview").ClassList);
        }
    }
}
