using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace TabBlazor.Components.QuickTables.Infrastructure;

internal static class AsyncQueryExecutorSupplier
{
    private static readonly ConcurrentDictionary<Type, bool> IsEntityFrameworkProviderTypeCache = new();

    public static IAsyncQueryExecutor GetAsyncQueryExecutor<T>(IServiceProvider services, IQueryable<T> queryable)
    {
        if (queryable is not null)
        {
            var executor = services.GetService<IAsyncQueryExecutor>();

            if (executor is null)
            {
                var providerType = queryable.Provider?.GetType();
                if (providerType is not null &&
                    IsEntityFrameworkProviderTypeCache.GetOrAdd(providerType, IsEntityFrameworkProviderType))
                {
                    throw new InvalidOperationException(
                        $"The supplied {nameof(IQueryable)} is provided by Entity Framework. To query it efficiently, you must reference the package TabBlazor.Components.QuickTables.EntityFrameworkAdapter and call AddQuickTableEntityFrameworkAdapter on your service collection.");
                }
            }
            else if (executor.IsSupported(queryable))
            {
                return executor;
            }
        }

        return null;
    }

    private static bool IsEntityFrameworkProviderType(Type queryableProviderType)
    {
        return queryableProviderType.GetInterfaces().Any(x =>
            string.Equals(x.FullName, "Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider"));
    }
}