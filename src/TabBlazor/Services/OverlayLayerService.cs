namespace TabBlazor.Services
{
    /// <summary>
    /// Shared z-index stack for overlays (modals, offcanvas). Each registered overlay is placed above every
    /// overlay already open, regardless of which service opened it. Registered by <c>AddTabBlazor</c>.
    /// </summary>
    public sealed class OverlayLayerService
    {
        private const int BaseZIndex = 1200;
        private const int ZIndexIncrement = 10;
        private readonly Dictionary<object, int> zIndexByOwner = [];

        /// <summary>Raised when an overlay is registered or unregistered.</summary>
        public event Action OnChanged;

        /// <summary>
        /// Places <paramref name="owner"/> on top of the stack and returns its z-index. Registering an owner
        /// that is already on the stack returns its existing z-index.
        /// </summary>
        public int Register(object owner)
        {
            if (zIndexByOwner.TryGetValue(owner, out var existingZIndex))
            {
                return existingZIndex;
            }

            var zIndex = zIndexByOwner.Count == 0 ? BaseZIndex : zIndexByOwner.Values.Max() + ZIndexIncrement;
            zIndexByOwner[owner] = zIndex;
            OnChanged?.Invoke();
            return zIndex;
        }

        /// <summary>Removes <paramref name="owner"/> from the stack.</summary>
        public void Unregister(object owner)
        {
            if (zIndexByOwner.Remove(owner))
            {
                OnChanged?.Invoke();
            }
        }

        /// <summary>Whether no registered overlay sits above <paramref name="zIndex"/>.</summary>
        public bool IsTopMost(int zIndex) => zIndexByOwner.Values.All(registeredZIndex => registeredZIndex <= zIndex);
    }
}
