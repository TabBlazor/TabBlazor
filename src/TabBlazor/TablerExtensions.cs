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

            var probe = new TablerOptions();
            tablerOptions(probe);

            if (probe.DefaultPositioning != Positioning.Default)
            {
                probe.EnablePopper = true;
                services.PostConfigure<TablerOptions>(o => o.EnablePopper = true);
            }

            services
                .AddScoped<ToastService>()
                .AddScoped<TablerService>()
                .AddScoped<IOffcanvasService, OffcanvasService>()
                .AddScoped<IModalService, ModalService>()
                .AddScoped<TableFilterService>()
                .AddScoped<IFormValidator, TablerDataAnnotationsValidator>()
                .AddSingleton<FlagService>();

            if (probe.EnablePopper)
            {
                services.AddScoped<IPopperService, PopperService>();
            }

            return services;
        }

        public static TabBlazorBuilder AddTabBlazor(this IServiceCollection services, Action<TablerOptions> tablerOptions = null)
        {
            services
                .AddTabler(tablerOptions);

            return new TabBlazorBuilder(services);
        }
    }
}