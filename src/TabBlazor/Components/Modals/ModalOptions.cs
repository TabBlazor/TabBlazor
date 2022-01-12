namespace TabBlazor
{
    public class ModalOptions
    {
        public bool ShowHeader { get; set; } = true;
        public bool ShowCloseButton { get; set; } = true;
        public bool Scrollable { get; set; } = true;
        public bool CloseOnClickOutside { get; set; } = false;
        public bool BlurBackground { get; set; } = true;
        public bool Backdrop { get; set; } = true;
        public bool CloseOnEsc { get; set; } = false;
        public bool Draggable { get; set; } = false;

        public ModalVerticalPosition VerticalPosition { get; set; }

        public ModalSize Size { get; set; } = ModalSize.Medium;
        public ModalFullscreen Fullscreen { get; set; } = ModalFullscreen.Never;

        public TablerColor? StatusColor { get; set; }
    }

    public enum ModalVerticalPosition
    {
        Default = 0,
        Centered = 1
    }
}
