using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualBasic;

namespace TabBlazor
{
    public partial class Dropdown : TablerBaseComponent
    {
        [Parameter] public RenderFragment DropdownTemplate { get; set; }
        [Parameter] public bool CloseOnClick { get; set; } = true;

        protected bool isExpanded;

        private double top;
        private double left;
        private bool isContextMenu;

        protected override string ClassNames => ClassBuilder
            .Add("dropdown")
            .Add("cursor-pointer")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();

        protected void OnClickOutside()
        {
            isExpanded = false;
        }

        private string GetSyle()
        {
            if (isContextMenu)
            {
                return $"position:fixed;top:{top}px;left:{left}px";
            }

            return "";
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

        public void Open()
        {
            isExpanded = true;
        }

        public void OpenAsContextMenu(MouseEventArgs e)
        {
            isContextMenu = true;
            top = e.ClientY;
            left = e.ClientX;
            isExpanded = true;
            StateHasChanged();

        }

        public void Close()
        {
            isExpanded = false;
        }
    }
}