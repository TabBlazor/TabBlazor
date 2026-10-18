namespace TabBlazor
{
    public class OffcanvasOptions
    {
        public bool Backdrop { get; set; } = true;
        public bool CloseOnClickOutside { get; set; } = false;
        public string WrapperCssClass { get; set; }
        public OffcanvasPosition Position { get; set; }
        public bool CloseOnEsc { get; set; } = false;
        /// <summary>When <c>true</c>, renders a narrower panel. Defaults to <c>false</c>.</summary>
        public bool Narrow { get; set; } = false;
        /// <summary>When <c>true</c>, detaches the panel from the screen edge with a small gap, border and rounded corners. Defaults to <c>false</c>.</summary>
        public bool Floating { get; set; } = false;
        /// <summary>The panel's width (start/end) or height (top/bottom). Defaults to <see cref="OffcanvasSize.Default"/>.</summary>
        public OffcanvasSize Size { get; set; } = OffcanvasSize.Default;
        /// <summary>
        /// Called before the panel closes by its close button, Esc or a click outside. Set
        /// <see cref="OffcanvasClosingEventArgs.Cancel"/> to keep it open, for example to ask "Discard changes?".
        /// </summary>
        public Func<OffcanvasClosingEventArgs, Task> OnClosing { get; set; }
    }

    /// <summary>The size of an offcanvas panel: its width at the start or end, its height at the top or bottom.</summary>
    public enum OffcanvasSize
    {
        /// <summary>Tabler's default size.</summary>
        Default = 0,
        Small = 1,
        Medium = 2,
        Large = 3,
        /// <summary>The whole screen, for example on phones.</summary>
        Full = 4
    }

    /// <summary>Passed to <c>OnClosing</c> handlers; set <see cref="Cancel"/> to keep the panel open.</summary>
    public sealed class OffcanvasClosingEventArgs
    {
        public bool Cancel { get; set; }
    }

    public enum OffcanvasPosition
    {
        Start = 0,
        End = 1,
        Top = 2,
        Bottom = 3
    }
}