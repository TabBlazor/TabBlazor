using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabBlazor
{
    public partial class ItemSelect<TItem, TValue> : TablerBaseComponent
    {
        /// <summary>
        /// List of items 
        /// </summary>
        [Parameter] public IEnumerable<TItem> Items { get; set; }

        /// <summary>
        /// Text to be displayed when no item is selected
        /// </summary>
        [Parameter] public string NoSelectedText { get; set; } = "*Select*";

        [Parameter] public string NoItemsText { get; set; }
        [Parameter] public bool ShowCheckBoxes { get; set; }
        [Parameter] public bool MultiSelect { get; set; }
        [Parameter] public bool Virtualize { get; set; }

        [Parameter] public List<TValue> SelectedValues { get; set; }
        [Parameter] public EventCallback<List<TValue>> SelectedValuesChanged { get; set; }

        [Parameter] public TValue SelectedValue { get; set; }
        [Parameter] public EventCallback<TValue> SelectedValueChanged { get; set; }

        [Parameter] public EventCallback Changed { get; set; }
        [Parameter] public EventCallback<bool> OnExpanded { get; set; }

        [Parameter] public Func<TItem, string> SelectedTextExpression { get; set; }
        [Parameter] public Func<TItem, string> IdExpression { get; set; }
        [Parameter] public Func<TItem, TValue> ConvertExpression { get; set; }
        [Parameter] public Func<TItem, bool> DisabledExpression { get; set; }
        [Parameter] public RenderFragment<TItem> ListTemplate { get; set; }
        [Parameter] public RenderFragment<List<TItem>> SelectedTemplate { get; set; }
        [Parameter] public RenderFragment FooterTemplate { get; set; }
        [Parameter] public RenderFragment HeaderTemplate { get; set; }
        [Parameter] public bool Clearable { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool RemoveSelectedFromList { get; set; }
        [Parameter] public int MaxSelectableItems { get; set; } = int.MaxValue;
        [Parameter] public Func<string, IEnumerable<TItem>> SearchMethod { get; set; }
        [Parameter] public string SearchPlaceholderText { get; set; }
        [Parameter] public string MaxListHeight { get; set; }
        [Parameter] public string ListWidth { get; set; }
        [Parameter] public string Label { get; set; }

        [Inject] private IJSRuntime jSRuntime { get; set; }
        private string? userAgent = null;
        private bool isFirefoxBrowser => userAgent.Contains("Firefox", StringComparison.InvariantCultureIgnoreCase);

        private bool showSearch => SearchMethod != null;
        private bool singleSelect => MaxSelectableItems == 1 || !MultiSelect;
        private List<TItem> selectedItems = new();
        private Dropdown dropdown;
        private string searchText;
        private TItem highlighted;

        protected override void OnInitialized()
        {
            if (ConvertExpression == null)
            {
                if (typeof(TItem) != typeof(TValue))
                {
                    throw new InvalidOperationException($"{GetType()} requires a {nameof(ConvertExpression)} parameter.");
                }

                ConvertExpression = item => item is TValue value ? value : default;
            }
        }

        protected override void OnParametersSet()
        {
            if (selectedItems == null) { selectedItems = new List<TItem>(); }
            selectedItems.Clear();

            //TODO How to handle if the items are in the provided list
            if (MultiSelect && SelectedValues != null)
            {
                if (typeof(TItem) == typeof(TValue))
                {
                    selectedItems = SelectedValues.Cast<TItem>().ToList();
                }
                else
                {
                    foreach (var selectedValue in SelectedValues)
                    {
                        AddSelectItemFromValue(selectedValue);
                    }
                }
            }
            else if (!MultiSelect && SelectedValue != null)
            {
                AddSelectItemFromValue(SelectedValue);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender && userAgent == null)
            {
                userAgent = await jSRuntime.InvokeAsync<string>("tabBlazor.getUserAgent");
            }
        }

        public bool IsExpanded => dropdown?.IsExpanded == true;

        private void DropdownExpanded(bool expanded)
        {
             OnExpanded.InvokeAsync(expanded);
        }

        private string GetListStyle()
        {
            var style = "";

            if (!string.IsNullOrWhiteSpace(MaxListHeight))
            {
                var overFlowStyle = isFirefoxBrowser ?
                    "overflow-y:scroll;" :
                    "overflow-y:overlay;";
                style = $"max-height:{MaxListHeight}; {overFlowStyle}";
            }

            if (!string.IsNullOrWhiteSpace(ListWidth))
            {
                style += $"width:{ListWidth}; overflow-x:overlay;border:none";
            }

            return style;
        }

        private void AddSelectItemFromValue(TValue value)
        {
            var item = Items.FirstOrDefault(e => EqualityComparer<TValue>.Default.Equals(ConvertExpression(e), value));
            if (item != null)
            {
                selectedItems.Add(item);
            }
        }

        protected List<TItem> FilteredList()
        {
            var filtered = Items;
            if (SearchMethod != null && !string.IsNullOrWhiteSpace(searchText))
            {
                filtered = SearchMethod(searchText).ToList();
            }

            if (RemoveSelectedFromList)
            {
                return filtered.Where(e => !selectedItems.Contains(e)).ToList();
            }
            return filtered.ToList();
        }

        private void ClearSearch()
        {
            searchText = string.Empty;
        }

        private async Task OnKey(KeyboardEventArgs e)
        {
            if (!dropdown.IsExpanded && (e.Key == "Enter" || e.Key == " "))
            {
                highlighted = default;
                dropdown.Open();
                SetHighlighted(1);
                return;
            }

            if (dropdown.IsExpanded)
            {
                if (e.Key == "Escape")
                {
                    highlighted = default;
                    dropdown.Close();
                }
                else if (e.Key == "ArrowDown")
                {
                    SetHighlighted(1);
                }
                else if (e.Key == "ArrowUp")
                {
                    SetHighlighted(-1);
                }
                else if (e.Key == "Enter" && highlighted != null)
                {
                    await ToogleSelected(highlighted);
                    SetHighlighted(1);
                }
            }
        }
        private void SetHighlighted(int step)
        {
            var myList = FilteredList();
            if (highlighted == null)
            {
                highlighted = myList.FirstOrDefault();
            }
            else
            {
                var pos = myList.IndexOf(highlighted);
                pos += step;
                highlighted = myList.ElementAtOrDefault(pos);
            }

            if (!CanSelect() && highlighted != null && !IsSelected(highlighted))
            {
                SetHighlighted(step);
            }
        }

        private string GetSelectedText(TItem item)
        {
            if (SelectedTextExpression == null) return item.ToString();
            return SelectedTextExpression.Invoke(item);
        }

        private bool IsDisabled(TItem item)
        {
            return DisabledExpression != null && DisabledExpression(item);
        }

        private bool CanSelect()
        {
            return singleSelect || MaxSelectableItems > selectedItems.Count;
        }

        private TValue GetValue(TItem item)
        {
            return ConvertExpression.Invoke(item);
        }

        private bool IsHighlighted(TItem item)
        {
            if (highlighted == null) { return false; }

            if (IdExpression != null)
            {
                return IdExpression.Invoke(highlighted) == IdExpression.Invoke(item);
            }

            return highlighted.Equals(item);
        }

        private bool IsSelected(TItem item)
        {
            if (IdExpression != null)
            {
                return selectedItems.FirstOrDefault(e => IdExpression.Invoke(e) == IdExpression.Invoke(item)) != null;
            }

            return selectedItems.Contains(item);
        }

        protected async Task RemoveSelected(TItem item)
        {
            if (IsSelected(item))
            {
                selectedItems.Remove(item);
            }
            dropdown.Close();
            await UpdateChanged();
        }

        public async Task ClearSelected()
        {
            selectedItems.Clear();
            dropdown.Close();
            await UpdateChanged();
        }

        protected async Task ToogleSelected(TItem item)
        {
            if (singleSelect)
            {
                selectedItems.Clear();
            }

            if (IsSelected(item))
            {
                selectedItems.Remove(item);
            }
            else
            {
                selectedItems.Add(item);

                if (singleSelect || !CanSelect())
                {
                    dropdown.Close();
                }
            }

            await UpdateChanged();
        }

        private async Task UpdateChanged()
        {
            //Allways send out SelectedValuesChanged
            var selectedValues = new List<TValue>();
            selectedValues = selectedItems.Select(e => GetValue(e)).ToList();
            await SelectedValuesChanged.InvokeAsync(selectedValues);

            if (!MultiSelect)
            {
                await SelectedValueChanged.InvokeAsync(selectedValues.FirstOrDefault());
            }

            await Changed.InvokeAsync();
        }

    }
}
