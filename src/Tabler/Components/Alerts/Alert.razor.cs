using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public partial class Alert : TablerBaseComponent
    {
        [Parameter] public string Title { get; set; }
        [Parameter] public bool Dismissible { get; set; }
        private bool dismissed;

        protected override string ClassNames => ClassBuilder
            .Add("alert")
            .Add(BackgroundColor.GetColorClass("alert"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("alert-dismissible", Dismissible)
            .ToString();

        protected void DismissAlert()
        {
            dismissed = true;
        }
    }
}