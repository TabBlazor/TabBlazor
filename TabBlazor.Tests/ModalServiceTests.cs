using System.Linq;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using TabBlazor.Services;
using Tabler.Docs.Components.Modals;

namespace TabBlazor.Tests
{
    public class ModalServiceTests : TabBlazorTestContext
    {
        private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

        private ModalService ShowModalAt(string url)
        {
            Navigation.NavigateTo(url);
            var service = new ModalService(Navigation);
            _ = service.ShowAsync("Title", new RenderComponent<TestModalContent>());
            return service;
        }

        [Fact]
        public void Keeps_modal_open_when_only_query_changes()
        {
            var service = ShowModalAt("/page");

            Navigation.NavigateTo("/page?tab=orders");

            Assert.Single(service.Modals);
        }

        [Fact]
        public void Closes_modal_when_path_changes()
        {
            var service = ShowModalAt("/page?tab=orders");

            Navigation.NavigateTo("/other");

            Assert.Empty(service.Modals);
        }

        [Fact]
        public void Closes_modal_when_path_changes_after_query_change()
        {
            var service = ShowModalAt("/page");

            Navigation.NavigateTo("/page?tab=orders");
            Navigation.NavigateTo("/other?tab=orders");

            Assert.Empty(service.Modals.ToList());
        }
    }
}
