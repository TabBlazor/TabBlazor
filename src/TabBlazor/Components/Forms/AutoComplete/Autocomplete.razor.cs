using System.Timers;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using TabBlazor.Services;
using Timer = System.Timers.Timer;

namespace TabBlazor;

/// <summary>
/// A text input with debounced async suggestions. Returns the typed string value (use <c>Typeahead</c> to bind
/// a selected object). Supply results via <see cref="SearchMethod"/>; suggestions can be grouped and templated.
/// <typeparamref name="TItem"/> is the suggestion item type.
/// </summary>
public partial class Autocomplete<TItem> : TablerBaseComponent, IAsyncDisposable
{
    [Inject] public TablerService TablerService { get; set; }
    [Inject] private IServiceProvider ServiceProvider { get; set; }

    [CascadingParameter] protected EditContext EditContext { get; set; }

    private string FieldCssClasses =>
        new ClassBuilder()
            .Add(ValidationClasses)
            .Add("form-control")
            .AddIf("form-control-flush", DisplayMode == DisplayMode.Flush)
            .AddIf(CssClass, CssClass != null)
            .ToString();

    private FieldIdentifier FieldIdentifier { get; set; }

    /// <summary>Additional CSS class(es) for the input.</summary>
    [Parameter] public string CssClass { get; set; }
    /// <summary>Controls the visual layout (e.g. flush). Defaults to <see cref="DisplayMode.Default"/>.</summary>
    [Parameter] public DisplayMode DisplayMode { get; set; } = DisplayMode.Default;
    /// <summary>Optional header text shown above the results list.</summary>
    [Parameter] public string ResultHeader { get; set; }
    /// <summary>Template rendered for each suggestion.</summary>
    [Parameter] public RenderFragment<TItem> ResultTemplate { get; set; }
    /// <summary>Template shown when no suggestions match.</summary>
    [Parameter] public RenderFragment NotFoundTemplate { get; set; }
    /// <summary>Raised when the text value changes.</summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    /// <summary>Identifies the bound field for validation; set automatically by <c>@bind-Value</c>.</summary>
    [Parameter] public Expression<Func<string>> ValueExpression { get; set; }
    /// <summary>Raised when the input loses focus.</summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { set; get; }
    /// <summary>The text value. Supports two-way binding via <c>@bind-Value</c>.</summary>
    [Parameter] public string Value { get; set; }
    /// <summary>Delay in milliseconds before searching after typing. Defaults to 300.</summary>
    [Parameter] public int Debounce { get; set; } = 300;
    /// <summary>Optional grouping key for suggestions.</summary>
    [Parameter] public Expression<Func<TItem, object>> GroupBy { get; set; }
    /// <summary>Projects a grouping key to its header text.</summary>
    [Parameter] public Func<object, string> GroupingHeaderExpression { get; set; }
    /// <summary>Template for group headers.</summary>
    [Parameter] public RenderFragment<object> GroupingHeaderTemplate { get; set; }

    /// <summary>Async function returning suggestions for the current search text. Required.</summary>
    [Parameter] public Func<string, Task<List<TItem>>> SearchMethod { get; set; }
    /// <summary>Raised when a suggestion is selected.</summary>
    [Parameter] public EventCallback<TItem> OnItemSelected { get; set; }
    /// <summary>When set, only the text after the last occurrence of this character is searched (e.g. for tags).</summary>
    [Parameter] public string SeparatorCharacter { get; set; }
    /// <summary>When true, the input is disabled. Defaults to false.</summary>
    [Parameter] public bool Disabled { get; set; } = false;
    /// <summary>When true, skips EditContext validation notifications. Defaults to false.</summary>
    [Parameter] public bool DisableValidation { get; set; }
    /// <summary>When true, shows suggestions as soon as the input is focused. Defaults to false.</summary>
    [Parameter] public bool ShowOptionOnFocus { get; set; }
    /// <summary>Placeholder text for the input.</summary>
    [Parameter] public string Placeholder { get; set; }
    /// <summary>Minimum characters before searching. Defaults to 2.</summary>
    [Parameter] public int MinimumLength { get; set; } = 2;
    /// <summary>Positioning strategy for the suggestions popup. When null, uses the configured default.</summary>
    [Parameter] public Positioning? Positioning { get; set; }
    /// <summary>Popper placement of the suggestions. Defaults to <see cref="Placement.BottomStart"/>.</summary>
    [Parameter] public Placement Placement { get; set; } = Placement.BottomStart;
    /// <summary>Popper offset in pixels. Defaults to 2.</summary>
    [Parameter] public int PopperOffset { get; set; } = 2;

    private Positioning EffectivePositioning =>
        Positioning ?? Options.CurrentValue.DefaultPositioning;

    private int SelectedIndex { get; set; } = -1;
    private List<TItem> Result { get; set; }
    private IEnumerable<IGrouping<object, TItem>> GroupedResult { get; set; }
    private List<TItem> ActualItems => Result ?? GroupedResult?.SelectMany(x => x).ToList();
    private bool IsShowingSuggestions { get; set; } = false;
    private Timer Timer { get; set; }

    private string searchText;
    private ElementReference _searchInput;
    private ElementReference _menuRef;
    private bool _eventsHookedUp;
    private string ValidationClasses => EditContext?.FieldCssClass(FieldIdentifier) ?? "";

    private IPopperService popperService;
    private IPopperInstance popperInstance;
    private bool UsePopper => EffectivePositioning != TabBlazor.Positioning.Default;

    protected override void OnInitialized()
    {
        if (ValueExpression != null)
        {
            FieldIdentifier = FieldIdentifier.Create(ValueExpression);
        }

        GroupingHeaderExpression ??= item => item.ToString();

        Timer = new Timer
        {
            Interval = Debounce,
            AutoReset = false
        };
        Timer.Elapsed += async (sender, args) => await DoSearch();

        if (UsePopper)
        {
            popperService = ServiceProvider.GetService(typeof(IPopperService)) as IPopperService
                ?? throw new InvalidOperationException(
                    "Popper not registered. Set TablerOptions.EnablePopper = true in AddTabBlazor.");
        }
    }

    protected override void OnParametersSet()
    {
        UpdateInput();
    }

    private async Task OnBlurTriggered()
    {
        await Task.Delay(300);
        await UpdateInput();
        IsShowingSuggestions = false;
        await InvokeAsync(StateHasChanged);
        await OnBlur.InvokeAsync();
    }

    private Task UpdateInput()
    {
        UnmatchedParameters ??= new Dictionary<string, object>();

        searchText = Value;

        UnmatchedParameters["class"] = FieldCssClasses;

        return Task.CompletedTask;
    }

    private async Task OnFocus()
    {
        if (ShowOptionOnFocus)
        {
            ForceShowOptions = true;
            SearchText ??= "";
            await DoSearch();
            ForceShowOptions = false;
        }
    }

    public bool ForceShowOptions { get; set; }

    private void InputChanged()
    {
        if (ForceShowOptions)
        {
            return;
        }
        
        if (MinimumLength > 0 && Value == null) return;

        searchText = GetSearchText(Value) ?? String.Empty;

        if (searchText.Length < MinimumLength)
        {
            Timer.Stop();
            SelectedIndex = -1;
            IsShowingSuggestions = false;
        }
        else if (searchText.Length >= MinimumLength)
        {
            Timer.Stop();
            Timer.Start();
        }

        ValueChanged.InvokeAsync(Value ?? String.Empty);
        UpdateInput();
        InvokeAsync(StateHasChanged);

        if (!DisableValidation)
            EditContext?.NotifyFieldChanged(FieldIdentifier);
    }

    private string SearchText
    {
        get => searchText;
        set
        {
            Value = value;
            InputChanged();
        }
    }

    private async Task OnItemSelectedCallback(TItem item)
    {
        if (OnItemSelected.HasDelegate)
        {
            await OnItemSelected.InvokeAsync(item);
        }

        if (!DisableValidation)
            EditContext?.NotifyFieldChanged(FieldIdentifier);

        IsShowingSuggestions = false;
        SelectedIndex = -1;
        searchText = "";
    }

    private bool SearchTextInAutoCompleteList(string input, IEnumerable<TItem> listResult)
    {
        return listResult.Any();
    }

    private async Task DoSearch()
    {
        var search = GetSearchText(SearchText);

        if (GroupBy != null)
        {
            GroupedResult = (await SearchMethod.Invoke(search)).GroupBy(GroupBy.Compile());
        }
        else
        {
            Result = await SearchMethod.Invoke(search ?? "");
        }

        if (NotFoundTemplate != null)
        {
            IsShowingSuggestions = true;
        }
        else
        {
            IsShowingSuggestions = Result?.Any() == true || GroupedResult?.Any() == true;
        }

        SelectedIndex = -1;
        await InvokeAsync(StateHasChanged);
    }

    private void OnClickOutside()
    {
        Close();
        StateHasChanged();
    }

    private void Close()
    {
        IsShowingSuggestions = false;
    }


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if ((firstRender && !Disabled) || (!_eventsHookedUp && !Disabled))
        {
            await TablerService.PreventDefaultKey(_searchInput, "keydown", new[] { "Enter" });
            _eventsHookedUp = true;
        }

        if (!UsePopper) return;

        if (IsShowingSuggestions && popperInstance == null)
        {
            popperInstance = await popperService.CreateAsync(_searchInput, _menuRef, new PopperOptions
            {
                Placement = Placement,
                Strategy = EffectivePositioning,
                Offset = PopperOffset
            });
            await popperInstance.ShowAsync();
        }
        else if (!IsShowingSuggestions && popperInstance != null)
        {
            await popperInstance.DisposeAsync();
            popperInstance = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Timer?.Dispose();
        if (popperInstance != null)
        {
            await popperInstance.DisposeAsync();
            popperInstance = null;
        }
    }

    private void MoveSelection(int count)
    {
        var index = SelectedIndex + count;

        if (index >= ActualItems.Count())
        {
            index = 0;
        }

        if (index < 0)
        {
            index = ActualItems.Count() - 1;
        }

        SelectedIndex = index;
    }
    
    private async Task HandleKeyup(KeyboardEventArgs args)
    {
        if (ActualItems == null)
            return;

        if (args.Key == "ArrowDown")
        {
            MoveSelection(1);
        }
        else if (args.Key == "ArrowUp")
        {
            MoveSelection(-1);
        }
        else if (args.Key == "Enter" && SelectedIndex >= 0 && SelectedIndex < ActualItems.Count)
        {
            await OnItemSelectedCallback(ActualItems[SelectedIndex]);
        }
        else if (args.Key == "Escape")
        {
            IsShowingSuggestions = false;
            SelectedIndex = -1;
        }
    }

    private string GetSelectedSuggestionClass(TItem item, int index)
    {
        const string resultClass = "active";

        if (Equals(item, Value))
        {
            if (index == SelectedIndex)
            {
                return resultClass;
            }

            return "";
        }

        if (index == SelectedIndex)
        {
            return resultClass;
        }

        return Equals(item, Value) ? resultClass : string.Empty;
    }

    private string GetSearchText(string value)
    {
        if (string.IsNullOrWhiteSpace(SeparatorCharacter))
            return value;

        var splitString = value.Split(SeparatorCharacter);
        if (splitString.Any())
            return splitString[^1].Trim();

        return string.Empty;
    }
}