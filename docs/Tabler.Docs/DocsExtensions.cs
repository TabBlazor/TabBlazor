using Microsoft.Extensions.DependencyInjection;
using TabBlazor;
using Tabler.Docs.Services;

namespace Tabler.Docs
{
    public static class DocsExtensions
    {
        public static IServiceCollection AddDocs(this IServiceCollection services)
        {
            return services
               .AddTabler()
               .AddSingleton<AppService>();
             
        }
    }
}
