using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    /// <summary>
    /// A contextual alert message box. Color is taken from <see cref="TablerBaseComponent.BackgroundColor"/>.
    /// </summary>
    public partial class Alert : TablerBaseComponent
    {
        /// <summary>The alert title shown in bold above the content.</summary>
        [Parameter] public string Title { get; set; }
        /// <summary>When true, shows a close button that dismisses the alert. Defaults to false.</summary>
        [Parameter] public bool Dismissible { get; set; }
        /// <summary>When true, renders the high-emphasis (solid) alert style. Defaults to false.</summary>
        [Parameter] public bool Important { get; set; }
        private bool dismissed;

        protected override string ClassNames => ClassBuilder
            .Add("alert")
            .Add(BackgroundColor.GetColorClass("alert"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("alert-dismissible", Dismissible)
            .AddIf("alert-important", Important)
            .ToString();

        protected void DismissAlert()
        {
            dismissed = true;
        }
    }
}