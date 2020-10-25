using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public partial class TablerAlert : TablerBaseComponent
    {
        [Parameter] public string Title { get; set; }
        [Parameter] public bool Dismissable { get; set; }
        private bool dismissed;

        protected override string ClassNames => ClassBuilder
            .Add("alert")
            .Add(BackgroundColor.GetColorClass("alert"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("alert-dismissible", Dismissable)
            .ToString();

        protected void DismissAlert()
        {
            dismissed = true;
        }
    }
}