using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using TabBlazor.Components.Tables.Components;
using TabBlazor.Components.Tables;
using TabBlazor.Services;

namespace TabBlazor
{
    public class TableBase<Item> : ComponentBase, IPopupEditTable<Item>, ITable<Item>, IInlineEditTable<Item>, IDetailsTable<Item>, ITableRow<Item>, ITableState<Item> // ITableRowActions<Item>
    {
        [Inject] private TablerService tabService { get; set; }
        [Inject] private IModalService modalService { get; set; }

        [Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object> UnknownParameters { get; set; }
        [Parameter] public bool ShowHeader { get; set; } = true;
        [Parameter] public bool ShowFooter { get; set; } = true;
        [Parameter] public bool ShowTableHeader { get; set; } = true;
        [Parameter] public bool Selectable { get; set; }
        [Parameter] public bool ShowNoItemsLabel { get; set; } = true;
        [Parameter] public string TableClass { get; set; } = "table card-table table-vcenter no-footer";
        [Parameter] public string ValidationRuleSet { get; set; } = "default";
        [Parameter] public int PageSize { get; set; } = 20;
        [Parameter] public IList<Item> Items { get; set; }
        [Parameter] public RenderFragment HeaderTemplate { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public RenderFragment<Item> DetailsTemplate { get; set; }
        [Parameter] public RenderFragment<Item> RowActionTemplate { get; set; }
        [Parameter] public RenderFragment<Item> RowActionEndTemplate { get; set; }
        [Parameter] public List<Item> SelectedItems { get; set; }
        [Parameter] public EventCallback<List<Item>> SelectedItemsChanged { get; set; }
        [Parameter] public bool MultiSelect { get; set; }
        [Parameter] public EventCallback<Item> OnRowClicked { get; set; }
        [Parameter] public Func<Task<IList<Item>>> OnRefresh { get; set; }
        [Parameter] public EventCallback<Item> OnItemEdited { get; set; }
        [Parameter] public EventCallback<Item> OnItemAdded { get; set; }
        [Parameter] public EventCallback<Item> OnItemDeleted { get; set; }
        [Parameter] public EventCallback<Item> OnItemSelected { get; set; }
        [Parameter] public Func<Item, bool> AllowDeleteExpression { get; set; }
        [Parameter] public Func<Item, bool> AllowEditExpression { get; set; }
        [Parameter] public int TotalCount { get; set; }
        [Parameter] public bool ShowCheckboxes { get; set; }
        [Parameter] public bool Hover { get; set; }
        [Parameter] public bool Responsive { get; set; }
        [Parameter] public Func<Task<Item>> AddItemFactory { get; set; }
        [Parameter] public Func<Item, Task<bool>> ConfirmDeleteFunction { get; set; }
        [Parameter] public bool KeyboardNavigation { get; set; }
        [Parameter] public bool ConfirmDelete { get; set; } = true;
        [Parameter] public TableEditMode EditMode { get; set; }

        public bool HasRowActions => RowActionTemplate != null || RowActionEndTemplate != null || AllowDelete || AllowEdit;

        public bool HasActionColumn => Columns.Any(e => e.ActionColumn);


        public bool ShowSearch { get; set; } = true;

        protected IEnumerable<TableResult<object, Item>> TempItems { get; set; } = Enumerable.Empty<TableResult<object, Item>>();
        public List<IColumn<Item>> Columns { get; } = new List<IColumn<Item>>();
        public List<IColumn<Item>> VisibleColumns => Columns.Where(x => x.Visible).ToList();
        public int PageNumber { get; set; }
        public int VisibleColumnCount => Columns.Count(x => x.Visible) + (HasRowActions ? 1 : 0) + (ShowCheckboxes ? 1 : 0);
        public bool IsAddInProgress { get; set; }
        public bool ReloadingItems { get; set; }
        public Item CurrentEditItem { get; private set; }
        protected IDictionary<string, object> Attributes { get; set; }
        public bool ChangedItem { get; set; }
        public bool AllowAdd => OnItemAdded.HasDelegate;
        public bool AllowDelete => OnItemDeleted.HasDelegate;
        public bool AllowEdit => OnItemEdited.HasDelegate;
        public bool HasGrouping => Columns.Any(x => x.GroupBy);
        public TheGridDataFactory<Item> DataFactory { get; set; }
        public Item SelectedItem { get; set; }

        protected ElementReference table;
        private bool tableInitialized;
        public string SearchText { get; set; }

        protected async override Task OnParametersSetAsync()
        {
            await Update();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender && !tableInitialized)
            {
                if (KeyboardNavigation)
                {
                    await tabService.PreventDefaultKey(table, "keydown", new string[] { "ArrowUp", "ArrowDown" });

                }
                tableInitialized = true;
                await Update();
            }
        }

        protected override void OnInitialized()
        {
            DataFactory = new TheGridDataFactory<Item>(Columns, this);

            if (Hover)
            {
                TableClass += " table-hover";
            }

            var baseAttributes = new Dictionary<string, object>()
            {
                { "class", TableClass }
            };

            if (UnknownParameters?.ContainsKey("class") == true)
            {
                baseAttributes["class"] = TableClass + " " + UnknownParameters["class"];
                baseAttributes.Union(UnknownParameters.Where(x => x.Key != "class") ?? new Dictionary<string, object>()).ToDictionary(x => x.Key, x => x.Value);
            }

            Attributes = baseAttributes;
        }

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
            catch (Exception e)
            {
                //HandleError("Something went wrong when searching", e);
                SearchText = "";
                await Update();
            }
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


        public bool IsSelected(Item item)
        {
            if (SelectedItems == null) { return false; }
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
            if (SelectedItems == null) { SelectedItems = new List<Item>(); }

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


        public async Task UnSelectAll()
        {
            SelectedItems.Clear();
            SelectedItem = default;
            await UpdateSelected();
        }

        public async Task SelectAll()
        {
            if (Items == null || !Items.Any()) return;

            SelectedItems = Items.ToList();
            SelectedItem = SelectedItems.First();
            await UpdateSelected();
        }

        private async Task UpdateSelected()
        {
            await OnItemSelected.InvokeAsync(SelectedItem);
            await SelectedItemsChanged.InvokeAsync(SelectedItems);
            await Update();
        }

        public async Task ClearSelectedItem()
        {
            //if (ChangedItem)
            //{
            //    ChangedItem = false;
            //    return;
            //}

            //SelectedItem = default;
            //await Update();
        }

        public async Task CancelEdit()
        {
            if (IsAddInProgress)
            {
                Items.Remove(CurrentEditItem);
            }

            await CloseEdit();
        }

        public async Task CloseEdit()
        {
            CurrentEditItem = default;
            IsAddInProgress = false;
            await Update();
        }

        public async Task Update(bool resetPage = false)
        {
            if (CurrentEditItem == null || !TempItems.Any())
            {
                TempItems = DataFactory.GetData(Items, resetPage);
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

        public async Task MoveToItem(Item item)
        {
            TempItems = DataFactory.GetData(Items, false, true, item);
            await Refresh();
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
            PageNumber = (int)Math.Ceiling((decimal)TotalCount / PageSize) - 1;
            await Update();
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
                tableItem = (Item)Activator.CreateInstance(typeof(Item));
            }

            Items.Add(tableItem);
            EditItem(tableItem);

            TempItems = DataFactory.GetData(Items, false, false, tableItem);
         
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
                var result = await modalService.ShowDialogAsync(new Components.Modals.DialogOptions
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
            CurrentEditItem = tableItem;
         
            //if (EditMode == TableEditMode.Popup)
            //{
            //    var renderComponent = new RenderComponent<PopupEdit<Item>>().Set(e => e.Table, this);
            //    var result = modalService.ShowAsync("Edit", renderComponent, new ModalOptions { Size = ModalSize.Large, ShowCloseButton=false });
            //}
          
            StateHasChanged();
        }

        public void SetPageSize(int pageSize)
        {
            PageSize = pageSize;
        }

        public string GetColumnWidth()
        {
            int width = 16; //(AllRowActions.Count * 25) + 16;
            return Math.Max(width, 80) + "px";
        }
    }
}
