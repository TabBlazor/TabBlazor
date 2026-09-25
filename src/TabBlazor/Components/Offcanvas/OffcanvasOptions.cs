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
    }

    public enum OffcanvasPosition
    {
        Start = 0,
        End = 1,
        Top = 2,
        Bottom = 3
    }
}