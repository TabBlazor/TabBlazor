namespace TabBlazor
{
    /// <summary>
    /// Controls where a table <c>Column</c> is shown: in the grid, in the edit form, or both.
    /// </summary>
    public enum ColumnVisibility
    {
        /// <summary>Shown both as a grid column and in the edit form. This is the default.</summary>
        ViewAndEdit = 0,
        /// <summary>Shown only as a grid column; omitted from the edit form.</summary>
        ViewOnly = 1,
        /// <summary>Shown only in the edit form (popup edit); omitted from the grid. Not supported in inline edit mode.</summary>
        EditOnly = 2
    }
}
