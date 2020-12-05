using Microsoft.Extensions.DependencyInjection;
using TabBlazor.Components;
using TabBlazor.Components.Tables;
using TabBlazor.Services;

namespace TabBlazor
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
