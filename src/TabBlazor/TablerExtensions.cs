using Microsoft.Extensions.DependencyInjection;
using TabBlazor.Components.Tables;
using TabBlazor.Services;

namespace TabBlazor
{
    public static class TablerExtensions
    {
        public static IServiceCollection AddTabler(this IServiceCollection services, Action<TablerOptions> tablerOptions = null)
        {
            if (tablerOptions is null)
            {
                tablerOptions = _ => { };
            }

            services.Configure(tablerOptions);

            return services
                .AddScoped<ToastService>()
                .AddScoped<TablerService>()
                .AddScoped<IOffcanvasService, OffcanvasService>()
                .AddScoped<IModalService, ModalService>()
                .AddScoped<TableFilterService>()
                .AddScoped<IFormValidator, TablerDataAnnotationsValidator>()
             .AddSingleton<FlagService>();
        }

        public static TabBlazorBuilder AddTabBlazor(this IServiceCollection services, Action<TablerOptions> tablerOptions = null)
        {
            services
                .AddTabler(tablerOptions);

            return new TabBlazorBuilder(services);
        }
    }
}