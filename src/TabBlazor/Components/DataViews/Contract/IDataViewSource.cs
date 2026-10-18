#nullable enable
namespace TabBlazor;

/// <summary>How a <see cref="IDataViewSource{TItem}"/> pages its data.</summary>
public enum PagingMode
{
    /// <summary>Skip/take paging: numbered pages, jumping to any page is allowed.</summary>
    Offset,
    /// <summary>Cursor paging: next and previous only; the view caches the cursor of each visited page.</summary>
    Cursor
}

/// <summary>
/// The one data contract behind every <c>DataView</c>: the view sends a <see cref="DataRequest"/> and gets a
/// <see cref="DataPage{TItem}"/> back. A source never sees UI objects and never changes view state.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
public interface IDataViewSource<TItem>
{
    /// <summary>The paging mode the source supports. The pager shows the matching controls.</summary>
    PagingMode PagingMode { get; }

    /// <summary>
    /// Loads one page. Implementations should pass <paramref name="cancellationToken"/> to every async call
    /// (for example <c>CountAsync(ct)</c> and <c>ToListAsync(ct)</c>), so a superseded request stops early.
    /// </summary>
    Task<DataPage<TItem>> GetPageAsync(DataRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Wraps a delegate as a <see cref="IDataViewSource{TItem}"/>. Used by <c>DataView.ItemsProvider</c>.
/// </summary>
public sealed class DelegateDataViewSource<TItem> : IDataViewSource<TItem>
{
    private readonly Func<DataRequest, CancellationToken, Task<DataPage<TItem>>> provider;

    public DelegateDataViewSource(Func<DataRequest, CancellationToken, Task<DataPage<TItem>>> provider,
        PagingMode pagingMode = PagingMode.Offset)
    {
        this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        PagingMode = pagingMode;
    }

    public PagingMode PagingMode { get; }

    public Task<DataPage<TItem>> GetPageAsync(DataRequest request, CancellationToken cancellationToken) =>
        provider(request, cancellationToken);
}
