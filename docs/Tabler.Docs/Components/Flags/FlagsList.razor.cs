using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TabBlazor;
using TabBlazor.Services;
using Tabler.Docs.Icons;

namespace Tabler.Docs.Components.Flags
{
    public partial class FlagsList : ComponentBase
    {
        [Inject] private FlagService flagServcie { get; set; }
        [Inject] private TablerService tabService { get; set; }
        [Inject] private ToastService toastService { get; set; }

        private List<GeneratedFlag> flags => flagServcie.AllFlags;
        private List<GeneratedFlag> filteredFlags = new();
        private List<GeneratedFlag> selectedFlags = new();

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


            IEnumerable<GeneratedFlag> query;
            query = flags;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(x => x.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase));
            }

            filteredFlags = query.ToList();
         
        }

       private int GetRowCount()
        {
                var flagSize = 100;
                var width = flagContainerRect?.Width ?? 100;
            var result = (int)Math.Floor((width / flagSize));
           return result;
           
        }

        private bool IsSelected(GeneratedFlag flagMember) => selectedFlags.Contains(flagMember);

        private void SelectFlag(GeneratedFlag flag)
        {
            if (IsSelected(flag))
            {
                selectedFlags.Remove(flag);
            }
            else
            {
                selectedFlags.Add(flag);
            }

        }

        private void ClearSelected()
        {
            selectedFlags.Clear(); 
        }

        private async Task CopyToClipboard()
        {
            var flags = "";
            foreach (var flag in selectedFlags)
            {
                flags += flag.DotNetProperty + Environment.NewLine;
            }

            await tabService.CopyToClipboard(flags);
            await toastService.AddToastAsync(new ToastModel { Title = $"{selectedFlags.Count} icons copied to clipboard", Options = new TabBlazor.ToastOptions { Delay = 2 } });
        }
    }
    
}
