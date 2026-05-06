using TabBlazor;

namespace Tabler.Docs.Services;

public class AppSettings
{
    public bool DarkMode { get; set; }
    public NavbarDirection NavbarDirection { get; set; } = NavbarDirection.Vertical;
    public NavbarBackground NavbarBackground { get; set; } = NavbarBackground.Dark;
}