using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualBasic;
using System.Threading.Tasks;

namespace TabBlazor
{
    public partial class Dropdown : TablerBaseComponent
    {
        [Parameter] public RenderFragment DropdownTemplate { get; set; }
        [Parameter] public bool CloseOnClick { get; set; } = true;

        public bool IsExpanded => isExpanded;

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
            if(isExpanded)
            {
                isExpanded = false;
            }
          
        }

        private string GetSyle()
        {
            if (isContextMenu)
            {
                return $"position:fixed;top:{top}px;left:{left}px";
            }

            return "";
        }

        protected async Task OnDropdownClick(MouseEventArgs e)
        {
            await OnClick.InvokeAsync(e);
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