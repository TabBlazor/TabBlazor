using Microsoft.AspNetCore.Components;

namespace TabBlazor;

public partial class Popover
{
    /// <summary>
    /// The trigger content. Clicking it toggles the popover.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// The content rendered inside the popover when it is open.
    /// </summary>
    [Parameter] public RenderFragment PopoverTemplate { get; set; }

    private bool showPopup = false;

    private void TooglePopup()
    {
        showPopup = !showPopup;
    }
}
