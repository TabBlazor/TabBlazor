using Microsoft.AspNetCore.Components;

namespace TabBlazor.Tests.Components
{
    public class DropdownTests : TabBlazorTestContext
    {
        private static RenderFragment Menu(bool closeOnClick = true) => builder =>
        {
            builder.OpenComponent<DropdownMenu>(0);
            builder.AddAttribute(1, nameof(DropdownMenu.ChildContent), (RenderFragment)(b =>
            {
                b.OpenComponent<DropdownItem>(0);
                b.AddAttribute(1, nameof(DropdownItem.ChildContent),
                    (RenderFragment)(c => c.AddContent(0, "Item")));
                b.CloseComponent();
            }));
            builder.CloseComponent();
        };

        private IRenderedComponent<Dropdown> RenderDropdown(bool closeOnClick = true)
        {
            return Render<Dropdown>(p => p
                .Add(d => d.CloseOnClick, closeOnClick)
                .AddChildContent("Trigger")
                .Add(d => d.DropdownTemplate, Menu()));
        }

        [Fact]
        public void Renders_dropdown_class()
        {
            var cut = RenderDropdown();
            Assert.Contains("dropdown", cut.Find("div.dropdown").ClassList);
        }

        [Fact]
        public void Menu_hidden_until_trigger_clicked()
        {
            var cut = RenderDropdown();
            Assert.Empty(cut.FindAll("a.dropdown-item"));
        }

        [Fact]
        public void Trigger_click_opens_menu()
        {
            var cut = RenderDropdown();

            cut.Find("div.dropdown").Click();

            Assert.True(cut.Instance.IsExpanded);
            Assert.Single(cut.FindAll("a.dropdown-item"));
        }

        [Fact]
        public void Item_click_closes_menu_when_close_on_click()
        {
            var cut = RenderDropdown(closeOnClick: true);
            cut.Find("div.dropdown").Click();

            cut.Find("a.dropdown-item").Click();

            Assert.False(cut.Instance.IsExpanded);
            Assert.Empty(cut.FindAll("a.dropdown-item"));
        }

        [Fact]
        public void Item_click_keeps_menu_open_when_not_close_on_click()
        {
            var cut = Render<Dropdown>(p => p
                .Add(d => d.CloseOnClick, false)
                .AddChildContent("Trigger")
                .Add(d => d.DropdownTemplate, Menu()));
            cut.Find("div.dropdown").Click();

            cut.Find("a.dropdown-item").Click();

            Assert.True(cut.Instance.IsExpanded);
            Assert.Single(cut.FindAll("a.dropdown-item"));
        }
    }
}
