using Microsoft.Extensions.DependencyInjection;
using Tabler.Components;
using Tabler.Components.Tables;
using Tabler.Services;

namespace Tabler
{
    public static class TablerExtensions
    {

        public static IServiceCollection AddTabler(this IServiceCollection services)
        {
            return services
               .AddScoped<ToastService>()
               .AddScoped<TablerService>()
               .AddScoped<IModalService, ModalService>()
               .AddScoped<TableFilterService>();
        }

    }
}
