using System.Timers;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using TabBlazor.Services;
using Timer = System.Timers.Timer;

namespace TabBlazor;

public partial class Autocomplete<TItem> : TablerBaseComponent, IDisposable
{
    [Inject]public TablerService TablerService { get; set; }
    
    [CascadingParameter] protected EditContext EditContext { get; set; }

    private string FieldCssClasses =>
        new ClassBuilder()
            .Add(ValidationClasses)
            .Add("form-control")
            .AddIf(CssClass, CssClass != null)
            .ToString();

    private FieldIdentifier FieldIdentifier { get; set; }

    [Parameter] public string CssClass { get; set; }
    [Parameter] public string ResultHeader { get; set; }
    [Parameter] public RenderFragment<TItem> ResultTemplate { get; set; }
    [Parameter] public RenderFragment NotFoundTemplate { get; set; }
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    [Parameter] public Expression<Func<string>> ValueExpression { get; set; }
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { set; get; }
    [Parameter] public string Value { get; set; }
    [Parameter] public int Debounce { get; set; } = 300;
    [Parameter] public Expression<Func<TItem, object>> GroupBy { get; set; }
    [Parameter] public Func<object, string> GroupingHeaderExpression { get; set; }
    [Parameter] public Func<string, Task<List<TItem>>> SearchMethod { get; set; }
    [Parameter] public EventCallback<TItem> OnItemSelected { get; set; }
    [Parameter] public string SeparatorCharacter { get; set; }
    [Parameter] public bool Disabled { get; set; } = false;
    [Parameter] public bool DisableValidation { get; set; }
    [Parameter] public bool ShowOptionOnFocus { get; set; }
    [Parameter] public string Placeholder { get; set; }
    [Parameter] public int MinimumLength { get; set; } = 2;

    private int SelectedIndex { get; set; } = -1;
    private List<TItem> Result { get; set; }
    private IEnumerable<IGrouping<object, TItem>> GroupedResult { get; set; }
    private List<TItem> ActualItems => Result ?? GroupedResult?.SelectMany(x => x).ToList();
    private bool IsShowingSuggestions { get; set; } = false;
    private Timer Timer { get; set; }

    private string searchText;
    private ElementReference _searchInput;
    private bool _eventsHookedUp;
    private string ValidationClasses => EditContext?.FieldCssClass(FieldIdentifier) ?? "";

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

    public void Dispose()
    {
        Timer.Dispose();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if ((firstRender && !Disabled) || (!_eventsHookedUp && !Disabled))
        {
            await TablerService.PreventDefaultKey(_searchInput, "keydown", new[] { "Enter" });
            _eventsHookedUp = true;
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