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

/// <summary>
/// A search-as-you-type selector that binds a chosen value. Unlike <c>Autocomplete</c>, it returns a strongly
/// typed selection. <typeparamref name="TItem"/> is the suggestion type; <typeparamref name="TValue"/> is the
/// bound value type — supply <see cref="ConvertExpression"/> when they differ.
/// </summary>
public partial class Typeahead<TItem, TValue> : TablerBaseComponent, IDisposable
{
    /// <summary>Async function returning suggestions for the search text. Required.</summary>
    [Parameter] public Func<string, Task<IEnumerable<TItem>>> SearchMethod { get; set; }
    /// <summary>The selected value. Supports two-way binding via <c>@bind-SelectedValue</c>.</summary>
    [Parameter] public TValue SelectedValue { get; set; }
    /// <summary>Raised when the selected value changes.</summary>
    [Parameter] public EventCallback<TValue> SelectedValueChanged { get; set; }
    /// <summary>Identifies the bound field for validation; set automatically by <c>@bind-SelectedValue</c>.</summary>
    [Parameter] public Expression<Func<TValue>> SelectedValueExpression { get; set; }
    /// <summary>Raised after the selection changes.</summary>
    [Parameter] public EventCallback Changed { get; set; }
    /// <summary>Delay in milliseconds before searching after typing. Defaults to 300.</summary>
    [Parameter] public int Debounce { get; set; } = 300;
    /// <summary>Minimum characters before searching. Defaults to 3.</summary>
    [Parameter] public int MinimumLength { get; set; } = 3;
    /// <summary>Maximum number of suggestions shown. Defaults to 20.</summary>
    [Parameter] public int MaximumItems { get; set; } = 20;
    /// <summary>Placeholder text for the search box.</summary>
    [Parameter] public string SearchPlaceholderText { get; set; } = "";
    /// <summary>Template rendered for each suggestion.</summary>
    [Parameter] public RenderFragment<TItem> ListTemplate { get; set; }
    /// <summary>Projects a suggestion item to its bound value. Required when <typeparamref name="TItem"/> and <typeparamref name="TValue"/> differ.</summary>
    [Parameter] public Func<TItem, TValue> ConvertExpression { get; set; }
    /// <summary>Projects a suggestion item to a unique id.</summary>
    [Parameter] public Func<TItem, string> IdExpression { get; set; }
    /// <summary>Optional max height (CSS) of the suggestions list.</summary>
    [Parameter] public string MaxListHeight { get; set; }
    /// <summary>Optional width (CSS) of the suggestions list.</summary>
    [Parameter] public string ListWidth { get; set; }
    /// <summary>Projects the selected value to its display text.</summary>
    [Parameter] public Func<TValue, string> SelectedTextExpression { get; set; }
    /// <summary>When true, shows suggestions as soon as the input is focused. Defaults to false.</summary>
    [Parameter] public bool ShowOptionOnFocus { get; set; }
    /// <summary>Controls the visual layout (e.g. flush). Defaults to <see cref="DisplayMode.Default"/>.</summary>
    [Parameter] public DisplayMode DisplayMode { get; set; } = DisplayMode.Default;
    /// <summary>Positioning strategy for the suggestions popup. When null, uses the configured default.</summary>
    [Parameter] public Positioning? Positioning { get; set; }
    /// <summary>Popper placement of the suggestions. Defaults to <see cref="Placement.BottomStart"/>.</summary>
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