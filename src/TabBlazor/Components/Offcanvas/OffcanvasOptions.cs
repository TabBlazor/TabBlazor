namespace TabBlazor
{
    public class OffcanvasOptions
    {
        public bool Backdrop { get; set; } = true;
        public bool CloseOnClickOutside { get; set; } = false;
        public string WrapperCssClass { get; set; }
        public OffcanvasPosition Position { get; set; }
        public bool CloseOnEsc { get; set; } = false;
    }

    public enum OffcanvasPosition
    {
        Start = 0,
        End = 1,
        Top = 2,
        Bottom = 3
    }
}