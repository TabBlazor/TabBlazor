using Microsoft.Extensions.DependencyInjection;
using Tabler.Services.Services;

namespace Tabler
{
    public static class TablerExtensions
    {

        public static IServiceCollection AddTabler(this IServiceCollection services)
        {
            return services
               .AddScoped<TablerToastService>()
               .AddScoped<TablerService>();
        }

    }
}
