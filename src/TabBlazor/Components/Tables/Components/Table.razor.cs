using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;
using TabBlazor.Components.Modals;
using TabBlazor.Components.Tables;
using TabBlazor.Components.Tables.Components;
using TabBlazor.Services;

namespace TabBlazor
{
    public class TableBase<Item> : ComponentBase, IPopupEditTable<Item>, ITable<Item>, IInlineEditTable<Item>, IDetailsTable<Item>, ITableRow<Item>, ITableState<Item>
    {
        private Item StateBeforeEdit;

        protected ElementReference table;
        private bool tableInitialized;
        [Inject] private TablerService tabService { get; set; }
        [Inject] private IModalService modalService { get; set; }
        [Inject] private IOptionsMonitor<TablerOptions> tablerOptions { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object> UnknownParameters { get; set; }

        [Parameter] public bool ShowHeader { get; set; } = true;
        [Parameter] public bool ShowTableHeader { get; set; } = true;
        [Parameter] public bool Selectable { get; set; }
        [Parameter] public bool ShowNoItemsLabel { get; set; } = true;
        [Parameter] public string TableClass { get; set; } = "table card-table table-vcenter no-footer";
        [Parameter] public string ValidationRuleSet { get; set; } = "default";
        [Parameter] public RenderFragment HeaderTemplate { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public EventCallback<Item> OnRowClicked { get; set; }
        [Parameter] public EventCallback<Item> OnBeforeEdit { get; set; }
        [Parameter] public EventCallback<Item> OnItemEdited { get; set; }
        [Parameter] public EventCallback<Item> OnItemAdded { get; set; }
        [Parameter] public EventCallback<Item> OnItemDeleted { get; set; }
        [Parameter] public bool Hover { get; set; }
        [Parameter] public bool Responsive { get; set; }
        [Parameter] public Func<Task<Item>> AddItemFactory { get; set; }
        [Parameter] public Func<Item, Task<bool>> ConfirmDeleteFunction { get; set; }
        [Parameter] public bool ConfirmDelete { get; set; } = true;
        [Parameter] public TableEditMode EditMode { get; set; }

        [Parameter] public OnCancelStrategy? CancelStrategy { get; set; }

        protected IEnumerable<TableResult<object, Item>> TempItems { get; set; } = Enumerable.Empty<TableResult<object, Item>>();
        public bool ReloadingItems { get; set; }
        protected IDictionary<string, object> Attributes { get; set; }
        public bool ChangedItem { get; set; }
        public bool AllowAdd => OnItemAdded.HasDelegate;
        public bool HasGrouping => Columns.Any(x => x.GroupBy);
        [Parameter] public RenderFragment<Item> DetailsTemplate { get; set; }
        [Parameter] public bool ShowCheckboxes { get; set; }
        [Parameter] public Action<TableEditPopupOptions<Item>> EditPopupMutator { get; set; }
        public bool IsRowValid { get; set; }
        public List<IColumn<Item>> Columns { get; } = new();
        public List<IColumn<Item>> VisibleColumns => Columns.Where(x => x.Visible).ToList();
        public bool IsAddInProgress { get; set; }
        public Item CurrentEditItem { get; private set; }

        public async Task OnValidSubmit(EditContext editContext)
        {
            if (IsAddInProgress)
            {
                await OnItemAdded.InvokeAsync(CurrentEditItem);
            }
            else
            {
                await OnItemEdited.InvokeAsync(CurrentEditItem);
            }

            await CloseEdit();
        }

        public async Task CancelEdit()
        {
            if (IsAddInProgress)
            {
                Items.Remove(CurrentEditItem);
            }

            if (StateBeforeEdit is not null)
            {
                var editItemIndex = Items.IndexOf(CurrentEditItem);
                Items.RemoveAt(editItemIndex);
                Items.Insert(editItemIndex, StateBeforeEdit);
            }

            await CloseEdit();
        }

        public async Task CloseEdit()
        {
            StateBeforeEdit = default;
            CurrentEditItem = default;
            IsAddInProgress = false;
            await Update();
        }

        [Parameter] public SelectAllStrategy SelectAllStrategy { get; set; } = SelectAllStrategy.AllPages;

        [Parameter] public bool ResetSortCycle { get; set; }
        [Parameter] public bool ShowFooter { get; set; } = true;
        [Parameter] public int PageSize { get; set; } = 20;
        [Parameter] public IList<Item> Items { get; set; }
        public IList<Item> CurrentItems => Items ?? TempItems?.FirstOrDefault();
        [Parameter] public RenderFragment<Item> RowActionTemplate { get; set; }
        [Parameter] public List<Item> SelectedItems { get; set; }
        [Parameter] public bool MultiSelect { get; set; }
        [Parameter] public Func<Task<IList<Item>>> OnRefresh { get; set; }
        [Parameter] public int TotalCount { get; set; }
        [Parameter] public IDataProvider<Item> DataProvider { get; set; }
        public bool HasRowActions => RowActionTemplate != null || RowActionEndTemplate != null || AllowDelete || AllowEdit;

        public bool HasActionColumn => Columns.Any(e => e.ActionColumn);

        public bool ShowSearch { get; set; } = true;
        public int PageNumber { get; set; }
        public int VisibleColumnCount => Columns.Count(x => x.Visible) + (HasRowActions ? 1 : 0) + (ShowCheckboxes ? 1 : 0);
        public string SearchText { get; set; }

        public async Task RefreshItems(MouseEventArgs args)
        {
            ReloadingItems = true;
            Items.Clear();
            await Update();
            Items = await OnRefresh();
            ReloadingItems = false;
            await Update();
        }

        public async Task Search(string searchText)
        {
            try
            {
                SearchText = searchText;
                await Update(true);
            }
            catch (Exception)
            {
                //HandleError("Something went wrong when searching", e);
                SearchText = "";
                await Update();
            }
        }

        public async Task UnSelectAll()
        {
            SelectedItems.Clear();
            SelectedItem = default;
            await UpdateSelected();
        }

        public async Task SelectAll()
        {
            if (CurrentItems == null || !CurrentItems.Any()) return;
            if (SelectAllStrategy == SelectAllStrategy.AllPages)
            {
                var currentPageNumber = PageNumber;
                var currentPageSize = PageSize;
                PageNumber = 0;
                PageSize = int.MaxValue;
                SelectedItems = DataProvider.GetData(Columns, this, null).Result.First();
                PageNumber = currentPageNumber;
                PageSize = currentPageSize;
            }
            else
            {
                SelectedItems = CurrentItems.ToList();
            }

            SelectedItem = SelectedItems.First();
            await UpdateSelected();
        }

        public Task ClearSelectedItem()
        {
            //if (ChangedItem)
            //{
            //    ChangedItem = false;
            //    return;
            //}

            //SelectedItem = default;
            //await Update();
            return Task.CompletedTask;
        }

        public async Task Update(bool resetPage = false)
        {
            if (CurrentEditItem == null || !TempItems.Any())
            {
                TempItems = await DataProvider.GetData(Columns, this, Items, resetPage);
                await Refresh();
            }
        }

        public void AddColumn(IColumn<Item> column)
        {
            Columns.Add(column);
            ShowSearch = Columns.Any(x => x.Searchable);
            StateHasChanged();
        }

        public void RemoveColumn(IColumn<Item> column)
        {
            Columns.Remove(column);
            StateHasChanged();
        }

        public async Task SetPage(int pageNumber)
        {
            PageNumber = pageNumber;
            await Update();
        }

        public async Task FirstPage()
        {
            if (PageNumber != 0)
            {
                PageNumber = 0;
                await Update();
            }
        }

        public async Task NextPage()
        {
            if (PageNumber < TotalCount / PageSize)
            {
                PageNumber++;
                await Update();
            }
        }

        public async Task PreviousPage()
        {
            if (PageNumber >= 1)
            {
                PageNumber--;
                await Update();
            }
        }

        public async Task LastPage()
        {
            PageNumber = (int) Math.Ceiling((decimal) TotalCount / PageSize) - 1;
            await Update();
        }

        public void SetPageSize(int pageSize)
        {
            PageSize = pageSize;
        }

        public string GetColumnWidth()
        {
            var width = 16; //(AllRowActions.Count * 25) + 16;
            return Math.Max(width, 80) + "px";
        }

        [Parameter] public RenderFragment<Item> RowActionEndTemplate { get; set; }
        [Parameter] public EventCallback<List<Item>> SelectedItemsChanged { get; set; }
        [Parameter] public EventCallback<Item> OnItemSelected { get; set; }
        [Parameter] public Func<Item, bool> AllowDeleteExpression { get; set; }
        [Parameter] public Func<Item, bool> AllowEditExpression { get; set; }
        [Parameter] public bool KeyboardNavigation { get; set; }
        public bool AllowDelete => OnItemDeleted.HasDelegate;
        public bool AllowEdit => OnItemEdited.HasDelegate && !IsAddInProgress;
        public Item SelectedItem { get; set; }

        public async Task RowClicked(Item item)
        {
            if (!ShowCheckboxes)
            {
                await SetSelectedItem(item);
            }

            await OnRowClicked.InvokeAsync(item);
        }

        public async Task SetSelectedItem(Item item)
        {
            if (SelectedItems == null)
            {
                SelectedItems = new List<Item>();
            }

            if (IsSelected(item))
            {
                SelectedItems.Remove(item);
                SelectedItem = default;
            }
            else
            {
                if (!MultiSelect)
                {
                    SelectedItems.Clear();
                }

                SelectedItems.Add(item);
            }

            SelectedItem = item;
            await UpdateSelected();
        }

        public async Task OnDeleteItem(Item item)
        {
            if (ConfirmDeleteFunction != null)
            {
                if (!await ConfirmDeleteFunction(item))
                {
                    return;
                }
            }
            else if (ConfirmDelete)
            {
                var result = await modalService.ShowDialogAsync(new DialogOptions
                {
                    MainText = "Are you sure you want to delete?",
                    IconType = InternalIcons.Alert_triangle,
                    StatusColor = TablerColor.Danger
                });
                if (!result)
                {
                    return;
                }
            }

            Items.Remove(item);
            await OnItemDeleted.InvokeAsync(item);
            await CloseEdit();
        }

        public void EditItem(Item tableItem)
        {
            var onCancelStrategy = CancelStrategy ?? tablerOptions.CurrentValue.DefaultOnCancelStrategy;
            if (!IsAddInProgress && onCancelStrategy == OnCancelStrategy.Revert)
            {
                StateBeforeEdit = tableItem.Copy();
            }

            if (OnBeforeEdit.HasDelegate)
            {
                OnBeforeEdit.InvokeAsync(tableItem);
            }

            CurrentEditItem = tableItem;
            StateHasChanged();
        }

        [Parameter] public bool UseNaturalSort { get; set; } = false;

        protected override async Task OnParametersSetAsync()
        {
            await Update();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender && !tableInitialized)
            {
                if (KeyboardNavigation)
                {
                    await tabService.PreventDefaultKey(table, "keydown", new[] {"ArrowUp", "ArrowDown"});
                }

                tableInitialized = true;
                await Update();
            }
        }

        protected override void OnInitialized()
        {
            DataProvider = DataProvider ?? new TheGridDataFactory<Item>();
            if (Hover)
            {
                TableClass += " table-hover";
            }

            var baseAttributes = new Dictionary<string, object>
            {
                {"class", TableClass}
            };
            if (UnknownParameters?.ContainsKey("class") == true)
            {
                baseAttributes["class"] = TableClass + " " + UnknownParameters["class"];
                baseAttributes.Union(UnknownParameters.Where(x => x.Key != "class") ?? new Dictionary<string, object>()).ToDictionary(x => x.Key, x => x.Value);
            }

            Attributes = baseAttributes;
        }

        public string GetTableCssClass()
        {
            var classBuileder = new ClassBuilder();
            return classBuileder
                .Add("tabler-table")
                .AddIf("grouped-table", HasGrouping)
                .AddIf("table-responsive", Responsive)
                .ToString();
        }

        public bool IsSelected(Item item)
        {
            if (SelectedItems == null)
            {
                return false;
            }

            return SelectedItems.Contains(item);
        }

        public bool IsEditingItem(Item item)
        {
            return CurrentEditItem != null && EqualityComparer<Item>.Default.Equals(item, CurrentEditItem);
        }

        protected bool ShowDetailsRow(Item item)
        {
            return DetailsTemplate != null && IsSelected(item);
        }

        private async Task UpdateSelected()
        {
            await OnItemSelected.InvokeAsync(SelectedItem);
            await SelectedItemsChanged.InvokeAsync(SelectedItems);
            await Update();
        }

        public async Task MoveToItem(Item item)
        {
            TempItems = await DataProvider.GetData(Columns, this, Items, false, true, item);
            await Refresh();
        }

        public async Task Refresh()
        {
            await InvokeAsync(StateHasChanged);
        }

        protected async Task OnAddItem()
        {
            if (IsAddInProgress)
            {
                return;
            }

            IsAddInProgress = true;
            Item tableItem;
            if (AddItemFactory != null)
            {
                tableItem = await AddItemFactory();
            }
            else
            {
                tableItem = (Item) Activator.CreateInstance(typeof(Item));
            }

            Items.Add(tableItem);
            EditItem(tableItem);
            TempItems = await DataProvider.GetData(Columns, this, Items, false, false, tableItem);
        }
    }

    public enum SelectAllStrategy
    {
        AllPages = 0,
        CurrentPage = 1
    }
}