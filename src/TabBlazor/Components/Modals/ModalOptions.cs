namespace TabBlazor
{
    public class ModalOptions
    {
        public bool ShowHeader { get; set; } = true;
        public bool Scrollable { get; set; } = true;
        public bool CloseOnClickOutside { get; set; } = false;
        public bool CloseOnEsc { get; set; } = false;
        public ModalSize Size { get; set; } = ModalSize.Medium;
        public ModalFullscreen Fullscreen { get; set; } = ModalFullscreen.Never;
    }
}
