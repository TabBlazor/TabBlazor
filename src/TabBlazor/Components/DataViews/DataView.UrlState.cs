#nullable enable
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace TabBlazor;

public partial class DataView<TItem>
{
    private string urlPrefix = string.Empty;
    private bool urlRegistered;
    private DataViewState? urlDefaults;
    private DataViewState? lastUrlState;
    private string? lastWrittenUri;
    private string? pendingCurrentKey;
    private bool notifyUrlStatePending;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Keeps paging, sort, search, filters, the view and the current item in the query string, so a view can be
    /// bookmarked, shared and reached with the back button. The view's <see cref="Id"/> is the key prefix.
    /// </summary>
    [Parameter] public bool UrlState { get; set; }

    /// <summary>Replace the history entry (default), or push one for page, sort and filter changes.</summary>
    [Parameter] public UrlHistoryMode UrlHistory { get; set; }

    private void OnInitializedUrlState()
    {
        if (!UrlState)
        {
            return;
        }

        urlPrefix = DataViewUrlState.Prefix(Id);
        Services.GetService<DataViewUrlRegistry>()?.Register(urlPrefix);
        urlRegistered = true;
        Navigation.LocationChanged += OnLocationChanged;
    }

    // Called once the columns are known, before the first load: the URL wins over the parameters.
    private void ApplyUrlStateOnFirstCollect()
    {
        if (!UrlState)
        {
            return;
        }

        urlDefaults = engine.GetState();
        var values = DataViewUrlState.FromUri(urlPrefix, Navigation.Uri, urlDefaults);
        if (values != null)
        {
            ApplyUrlValues(values);
            notifyUrlStatePending = true;
        }

        lastUrlState = engine.GetState();
    }

    // Bound State and Filters learn about state read from the URL once the view is interactive.
    private async Task NotifyUrlStateAsync()
    {
        if (!notifyUrlStatePending)
        {
            return;
        }

        notifyUrlStatePending = false;
        lastState = engine.GetState();
        await StateChanged.InvokeAsync(lastState);
        await NotifyFiltersChangedAsync(lastFiltersParameter ?? []);
    }

    private void ApplyUrlValues(DataViewUrlValues values)
    {
        CancelFilterDelays();
        engine.ApplyState(values.State);
        if (values.View is "list" && Views.HasFlag(DataViewKind.List))
        {
            chosenView = DataViewKind.List;
        }
        else if (values.View is "table" && Views.HasFlag(DataViewKind.Table))
        {
            chosenView = DataViewKind.Table;
        }
        else if (values.View == null)
        {
            chosenView = null;
        }

        pendingCurrentKey = engine.KeyGetter == null ? null : values.Current;
        if (pendingCurrentKey == null)
        {
            currentKey = null;
            currentItem = default;
        }
    }

    private string? ViewForUrl
    {
        get
        {
            var current = CurrentView;
            return current == SingleView(View ?? Views) ? null : current == DataViewKind.List ? "list" : "table";
        }
    }

    // Only a real key goes into the URL: without a KeySelector the row itself would (its ToString, or every field of a record).
    private string? CurrentForUrl =>
        currentKey != null && engine.KeyGetter != null &&
        EffectiveDetailsMode is DataDetailsMode.Drawer or DataDetailsMode.SidePanel
            ? DataFilters.FormatValue(currentKey)
            : null;

    /// <summary>Writes the current state to the URL (only when it changed).</summary>
    private void WriteUrlState()
    {
        if (!UrlState || urlDefaults == null || disposed)
        {
            return;
        }

        var state = engine.GetState();
        var query = DataViewUrlState.ToQuery(urlPrefix, state, urlDefaults, engine.Columns.Where(c => c.Filterable).Select(c => c.Id),
            ViewForUrl, CurrentForUrl);
        var uri = Navigation.GetUriWithQueryParameters(query);
        if (uri == Navigation.Uri)
        {
            return;
        }

        // Page, sort and filter changes can add a history entry; typing a search never does.
        var push = UrlHistory == UrlHistoryMode.Push && lastUrlState != null &&
                   string.Equals(lastUrlState.Search ?? string.Empty, state.Search ?? string.Empty, StringComparison.Ordinal) &&
                   !state.Equals(lastUrlState);
        lastUrlState = state;
        lastWrittenUri = uri;
        Navigation.NavigateTo(uri, new NavigationOptions { ReplaceHistoryEntry = !push });
    }

    // Back and forward: apply the state from the URL and reload.
    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (e.Location == lastWrittenUri || urlDefaults == null)
        {
            return;
        }

        _ = InvokeAsync(async () =>
        {
            var values = DataViewUrlState.FromUri(urlPrefix, e.Location, urlDefaults)
                         ?? new DataViewUrlValues(urlDefaults, null, null);
            var before = engine.GetState();
            var viewBefore = CurrentView;
            ApplyUrlValues(values);
            lastUrlState = engine.GetState();
            if (!lastUrlState.Equals(before))
            {
                lastState = lastUrlState;
                await StateChanged.InvokeAsync(lastState);
                await NotifyFiltersChangedAsync(before.Filters);
                await engine.LoadAsync();
            }
            else if (viewBefore != CurrentView || pendingCurrentKey != null)
            {
                ApplyPendingCurrent(engine.Items);
                StateHasChanged();
            }
        });
    }

    // The current item from the URL is matched by key once its page has loaded.
    private void ApplyPendingCurrent(IEnumerable<TItem> items)
    {
        if (pendingCurrentKey == null)
        {
            return;
        }

        foreach (var item in items)
        {
            if (DataFilters.FormatValue(KeyOf(item)) == pendingCurrentKey)
            {
                pendingCurrentKey = null;
                currentKey = KeyOf(item);
                currentItem = item;
                _ = EnsureDetailsLoadedAsync(item);
                return;
            }
        }
    }

    private void DisposeUrlState()
    {
        if (!urlRegistered)
        {
            return;
        }

        urlRegistered = false;
        Navigation.LocationChanged -= OnLocationChanged;
        Services.GetService<DataViewUrlRegistry>()?.Unregister(urlPrefix);
    }
}
