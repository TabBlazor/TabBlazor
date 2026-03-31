namespace TabBlazor;

public enum PageBreakpoint
{
    None,
    Always,
    Sm,
    Md,
    Lg,
    Xl,
    Xxl
}

public static class PageSizeExtensions
{
    public static string ToNavbarExpandClass(this PageBreakpoint pageBreakpoint) => pageBreakpoint switch
    {
        PageBreakpoint.Always => "",
        PageBreakpoint.Sm => "navbar-expand-md",
        PageBreakpoint.Md => "navbar-expand-lg",
        PageBreakpoint.Lg => "navbar-expand-xl",
        PageBreakpoint.Xl => "navbar-expand-xxl",
        PageBreakpoint.Xxl => "navbar-expand-xxl",
        PageBreakpoint.None => "navbar-expand",
        _ => string.Empty
    };
}

