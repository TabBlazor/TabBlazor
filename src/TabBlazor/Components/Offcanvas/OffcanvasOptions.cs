
namespace TabBlazor
{
    public class OffcanvasOptions
    {
        public bool Backdrop { get; set; } = true;
        public OffcanvasPosition Position { get; set; }
    }
    public enum OffcanvasPosition
    {
        Start = 0,
        End = 1,
        Top = 2,
        Bottom = 3
    }

}
