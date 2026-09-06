namespace TabBlazor;

/// <summary>How a vertical <see cref="Navbar"/> folds down to an icon rail.</summary>
public enum NavbarFold
{
    /// <summary>Full-width sidebar.</summary>
    None,
    /// <summary>Always folded to an icon rail; sub-menus open as flyouts.</summary>
    Folded,
    /// <summary>Folded to an icon rail that expands while hovered or focused.</summary>
    FoldedHover
}
