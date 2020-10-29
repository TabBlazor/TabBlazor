using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Tabler.Components
{
    public partial class TablerDropDown : TablerBaseComponent
    {
        [Parameter] public RenderFragment Dropdown { get; set; }
        [Parameter] public string Text { get; set; }
        protected bool isExpanded;

        protected override string ClassNames => ClassBuilder
            .Add("dropdown")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();

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