using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tabler
{
    public static class TablerExtensions
    {

        public static IServiceCollection AddTabler(this IServiceCollection services)
        {
            return services
               .AddScoped<TablerToastService>();
        }

    }
}
