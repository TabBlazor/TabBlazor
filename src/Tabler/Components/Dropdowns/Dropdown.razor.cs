using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Tabler.Components
{
    public partial class Dropdown : TablerBaseComponent
    {
        [Parameter] public RenderFragment DropdownTemplate { get; set; }
        [Parameter] public string Text { get; set; }
        protected bool isExpanded;

        protected override string ClassNames => ClassBuilder
            .Add("dropdown")
            .Add("clickable")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();

        protected void OnClickOutside()
        {
            isExpanded = false;
        }

        protected void OnDropdownClick(MouseEventArgs e)
        {
            OnClick.InvokeAsync(e);
            Toogle();
        }

        public void Toogle()
        {
            isExpanded = !isExpanded;
        }
    }
}