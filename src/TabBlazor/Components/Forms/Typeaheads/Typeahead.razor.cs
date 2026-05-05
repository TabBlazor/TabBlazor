using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Components.Forms;
using TabBlazor.Services;
using Timer = System.Timers.Timer;

namespace TabBlazor;

public partial class Typeahead<TItem, TValue> : TablerBaseComponent, IDisposable
{
    [Parameter] public Func<string, Task<IEnumerable<TItem>>> SearchMethod { get; set; }
    [Parameter] public TValue SelectedValue { get; set; }
    [Parameter] public EventCallback<TValue> SelectedValueChanged { get; set; }
    [Parameter] public Expression<Func<TValue>> SelectedValueExpression { get; set; }
    [Parameter] public EventCallback Changed { get; set; }
    [Parameter] public int Debounce { get; set; } = 300;
    [Parameter] public int MinimumLength { get; set; } = 3;
    [Parameter] public int MaximumItems { get; set; } = 20;
    [Parameter] public string SearchPlaceholderText { get; set; } = "";
    [Parameter] public RenderFragment<TItem> ListTemplate { get; set; }
    [Parameter] public Func<TItem, TValue> ConvertExpression { get; set; }
    [Parameter] public Func<TItem, string> IdExpression { get; set; }
    [Parameter] public string MaxListHeight { get; set; }
    [Parameter] public string ListWidth { get; set; }
    [Parameter] public Func<TValue, string> SelectedTextExpression { get; set; }
    [Parameter] public bool ShowOptionOnFocus { get; set; }
    [Parameter] public DisplayMode DisplayMode { get; set; } = DisplayMode.Default;
    [Parameter] public Positioning Positioning { get; set; } = Positioning.Default;
    [Parameter] public Placement Placement { get; set; } = Placement.BottomStart;

    [CascadingParameter] private EditContext CascadedEditContext { get; set; }

    [Inject] private TablerService TablerService { get; set; }

    private string FieldCssClasses { get; set; }
    private string InputCssClasses => new ClassBuilder()
        .Add("form-control")
        .Add(FieldCssClasses)
        .AddIf("form-control-flush", DisplayMode == DisplayMode.Flush)
        .ToString();
    private FieldIdentifier? fieldIdentifier;
    private IEnumerable<TItem> listItems;
    private string searchText;
    private Timer debounceTimer;
    private bool isSearching;
    private Dropdown dropdown;
    private ElementReference input;
    private bool isInput;
    private bool setFocus;
    private int highlightedIndex = -1;
    private bool eventsHookedUp;

    protected override void OnInitialized()
    {
        debounceTimer = new Timer
        {
            Interval = Debounce,
            AutoReset = false
        };
        debounceTimer.Elapsed += async (_, _) =>  await Search();

        if (ConvertExpression == null)
        {
            if (typeof(TItem) != typeof(TValue))
            {
                throw new InvalidOperationException($"{GetType()} requires a {nameof(ConvertExpression)} parameter.");
            }

            ConvertExpression = item => item is TValue value ? value : default;
        }
        
        if (SelectedValueExpression != null)
        {
            fieldIdentifier = FieldIdentifier.Create(SelectedValueExpression);
        }
        
        if (CascadedEditContext != null)
        {
            CascadedEditContext.OnValidationStateChanged += SetValidationClasses;
        }
    }
    
    private void SetValidationClasses(object sender, ValidationStateChangedEventArgs args)
    {
        if (fieldIdentifier is not { } fid) { return; }
        FieldCssClasses = CascadedEditContext?.FieldCssClass(fid) ?? "";
    }

    protected override void OnAfterRender(bool firstRender)
    {
        Validate();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (setFocus)
        {
            await input.FocusAsync();
            setFocus = false;
        }

        if (!eventsHookedUp && isInput)
        {
            await TablerService.PreventDefaultKey(input, "keydown", new[] { "Enter", "ArrowUp", "ArrowDown" });
            eventsHookedUp = true;
        }
    }

    private async Task HandleKey(KeyboardEventArgs args)
    {
        if (listItems == null || !listItems.Any() || dropdown?.IsExpanded != true)
        {
            if (args.Key == "Escape") dropdown?.Close();
            return;
        }

        var items = listItems.ToList();

        if (args.Key == "ArrowDown")
        {
            highlightedIndex = (highlightedIndex + 1) % items.Count;
        }
        else if (args.Key == "ArrowUp")
        {
            highlightedIndex = highlightedIndex <= 0 ? items.Count - 1 : highlightedIndex - 1;
        }
        else if (args.Key == "Enter" && highlightedIndex >= 0 && highlightedIndex < items.Count)
        {
            await SelectItem(items[highlightedIndex]);
        }
        else if (args.Key == "Escape")
        {
            dropdown.Close();
            highlightedIndex = -1;
        }
    }

    private async Task SetInput(bool value)
    {
        isInput = value;
        setFocus = value;

        if ((listItems == null || !listItems.Any()))
        {
            ClearSearch();
        }

        if (value && ShowOptionOnFocus)
        {
            SearchText ??= "";
            await ExecuteSearchASync();
        }
    }

    private string SearchText
    {
        get => searchText;
        set
        {
            searchText = value;

            if (value.Length == 0)
            {
                debounceTimer.Stop();
                dropdown.Close();
            }
            else
            {
                debounceTimer.Stop();
                debounceTimer.Start();
            }
        }
    }

    private async Task ClearSelected()
    {
        SelectedValue = default;
        await SelectedValueChanged.InvokeAsync(SelectedValue);
        dropdown.Close();
        Validate();
    }

    private void ClearSearch()
    {
        SearchText = "";
        dropdown.Close();
    }

    private bool IsSelected(TItem item)
    {
        if (SelectedValue == null) { return false; }
        return SelectedValue.Equals(ConvertExpression(item));
    }

    private string GetListStyle()
    {
        var style = "";

        if (!string.IsNullOrWhiteSpace(MaxListHeight))
        {
            style = $"max-height:{MaxListHeight}; overflow-y:auto;";
        }

        if (!string.IsNullOrWhiteSpace(ListWidth))
        {
            style += $"width:{ListWidth}; overflow-x:auto;border:none";
        }

        return style;
    }

    private async Task SelectItem(TItem item)
    {
        SelectedValue = ConvertExpression(item);
        searchText = "";
        highlightedIndex = -1;
        dropdown.Close();
        await SelectedValueChanged.InvokeAsync(SelectedValue);
        Validate();
    }

    private async Task Search()
    {
        if (searchText.Length < MinimumLength)
        {
            dropdown.Close();
            await InvokeAsync(StateHasChanged);
            return;
        }

        await ExecuteSearchASync();
    }

    private async Task ExecuteSearchASync()
    {
        isSearching = true;
        highlightedIndex = -1;
        dropdown.Open();
        await InvokeAsync(StateHasChanged);
        listItems = (await SearchMethod?.Invoke(searchText)).Take(MaximumItems).ToArray();
        isSearching = false;
        await InvokeAsync(StateHasChanged);
    }

    private void Validate()
    {
        if (fieldIdentifier is not { } fid)
        {
            return;
        }
        CascadedEditContext?.NotifyFieldChanged(fid);
        CascadedEditContext?.Validate();
    }

    public void Dispose()
    {
        if (CascadedEditContext != null)
        {
            CascadedEditContext.OnValidationStateChanged -= SetValidationClasses;
        }
    }
}