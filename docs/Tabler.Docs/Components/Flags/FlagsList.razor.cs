using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TabBlazor;
using TabBlazor.Services;

namespace Tabler.Docs.Components.Flags
{
    public partial class FlagsList : ComponentBase
    {
        [Inject] private FlagService flagServcie { get; set; }
        [Inject] private TablerService tabService { get; set; }
        [Inject] private ToastService toastService { get; set; }

        private List<FlagMember> flagMembers => flagServcie.AllFlagMembers;
        private List<FlagMember> filteredFlags = new();
        private List<FlagMember> selectedFlags = new();

        private string searchText;
        private ContentRect flagContainerRect;

        protected override void OnInitialized()
        {
            SearchFlags();

        }

        private void FlagContainerResized(ResizeObserverEntry resizeObserverEntry)
        {
            flagContainerRect = resizeObserverEntry.ContentRect;
        }

        private void Search(ChangeEventArgs e)
        {
            searchText = e.Value.ToString();
            SearchFlags();
        }


        private void SearchFlags()
        {

            filteredFlags.Clear();


            IEnumerable<FlagMember> query;
            query = flagMembers;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(x => x.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase));
            }


            filteredFlags = query.ToList();
            //filteredIcons = query.ToList();

        }

        private bool IsSelected(FlagMember flagMember) => selectedFlags.Contains(flagMember);

        //private int GetRowCount()
        //{
        //    var iconSize = size + 30;
        //    var width = iconContainerRect?.Width ?? 100;

        //    return (int)Math.Floor((width / iconSize));
        //}

        private void SelectFlag(FlagMember flagMember)
        {
            if (IsSelected(flagMember))
            {
                selectedFlags.Remove(flagMember);
            }
            else
            {
                selectedFlags.Add(flagMember);
            }

        }

        private void ClearSelected()
        {
            selectedFlags.Clear(); 
        }

        private async Task CopyToClipboard()
        {
            //    var iconlist = "";
            //    foreach (var icon in selectedIcons)
            //    {
            //        iconlist += icon.DotNetProperty + Environment.NewLine;
            //    }

            //    await tabService.CopyToClipboard(iconlist);
            //    await toastService.AddToastAsync(new ToastModel { Title = $"{selectedIcons.Count} icons copied to clipboard", Options = new TabBlazor.ToastOptions { Delay = 2 } });
            //}
        }
        //}

        //public class ListIcon
        //{
        //    public string Name { get; set; }
        //    public IIconType IconType { get; set; }

        //    public string DotNetProperty => $"public static IIconType {Name} => new {IconType.ClassName}(@\"{IconType?.Elements}\");";


        //}

    }
}
