using Microsoft.Extensions.DependencyInjection;
using TabBlazor.Components.QuickTables.Infrastructure;

namespace TabBlazor.QuickTable.EntityFramework;

/// <summary>
///     Provides extension methods to configure <see cref="IAsyncQueryExecutor" /> on a <see cref="IServiceCollection" />.
/// </summary>
public static class EntityFrameworkAdapterServiceCollectionExtensions
{
    /// <summary>
    ///     Registers an Entity Framework aware implementation of <see cref="IAsyncQueryExecutor" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    public static void AddQuickTableEntityFrameworkAdapter(this IServiceCollection services)
    {
        services.AddSingleton<IAsyncQueryExecutor, EntityFrameworkAsyncQueryExecutor>();
    }
}