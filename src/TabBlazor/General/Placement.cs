namespace TabBlazor;

public enum Placement
{
    Auto,
    AutoStart,
    AutoEnd,
    Top,
    TopStart,
    TopEnd,
    Bottom,
    BottomStart,
    BottomEnd,
    Right,
    RightStart,
    RightEnd,
    Left,
    LeftStart,
    LeftEnd
}

internal static class PlacementExtensions
{
    public static string ToPopperString(this Placement p) => p switch
    {
        Placement.Auto => "auto",
        Placement.AutoStart => "auto-start",
        Placement.AutoEnd => "auto-end",
        Placement.Top => "top",
        Placement.TopStart => "top-start",
        Placement.TopEnd => "top-end",
        Placement.Bottom => "bottom",
        Placement.BottomStart => "bottom-start",
        Placement.BottomEnd => "bottom-end",
        Placement.Right => "right",
        Placement.RightStart => "right-start",
        Placement.RightEnd => "right-end",
        Placement.Left => "left",
        Placement.LeftStart => "left-start",
        Placement.LeftEnd => "left-end",
        _ => "top"
    };
}
