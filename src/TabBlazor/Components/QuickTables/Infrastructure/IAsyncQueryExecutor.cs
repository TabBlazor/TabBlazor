namespace TabBlazor.Components.QuickTables.Infrastructure;

public interface IAsyncQueryExecutor
{
    bool IsSupported<T>(IQueryable<T> queryable);
    Task<int> CountAsync<T>(IQueryable<T> queryable);
    Task<T[]> ToArrayAsync<T>(IQueryable<T> queryable);
}