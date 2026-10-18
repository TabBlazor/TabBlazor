#nullable enable
namespace TabBlazor;

/// <summary>The views a DataView can show its items in. Combine flags in <c>Views</c> to offer a view switcher.</summary>
[Flags]
public enum DataViewKind
{
    None = 0,

    /// <summary>Rows and columns.</summary>
    Table = 1,

    /// <summary>One or two lines per item: avatar or icon, title, subtitle, meta on the right.</summary>
    List = 2
}

/// <summary>What a view shows in place of its items.</summary>
public enum DataViewStatus
{
    /// <summary>The items of the current page.</summary>
    Items,

    /// <summary>The first load has not finished.</summary>
    FirstLoad,

    /// <summary>There is no data at all.</summary>
    Empty,

    /// <summary>No rows match the search or filters.</summary>
    NoResults,

    /// <summary>Loading failed.</summary>
    Error
}
