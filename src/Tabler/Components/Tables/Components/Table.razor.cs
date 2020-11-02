using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Tabler.Components.Tables.Components;

namespace Tabler.Components.Tables
{
    public class TableBase<Item> : ComponentBase,  ITable<Item>, IInlineEditTable<Item>, IDetailsTable<Item>, ITableRow<Item>, ITableState // ITableRowActions<Item>
    {
       // [Inject] protected AppService AppService { get; set; }

        [Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object> UnknownParameters { get; set; }
        //[Parameter] public List<MenuDropdownItem<Item>> RowActions { get; set; } = new List<MenuDropdownItem<Item>>();
        //[Parameter] public List<MenuDropdownItem<object>> HeaderActions { get; set; } = new List<MenuDropdownItem<object>>();
        [Parameter] public bool ShowHeader { get; set; } = true;
        [Parameter] public bool ShowFooter { get; set; } = true;
        [Parameter] public bool ShowTableHeader { get; set; } = true;
        [Parameter] public bool Selectable { get; set; }
        [Parameter] public bool ShowNoItemsLabel { get; set; } = true;
        //[Parameter] public WebColor? StatusColor { get; set; }
        [Parameter] public string TableClass { get; set; } = "table card-table table-striped table-vcenter datatable dataTable no-footer";
        [Parameter] public string ValidationRuleSet { get; set; } = "default";
        [Parameter] public int PageSize { get; set; } = 20;
        [Parameter] public IList<Item> Items { get; set; }

        [Parameter] public RenderFragment HeaderTemplate { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public RenderFragment<Item> DetailsTemplate { get; set; }

        [Parameter] public Func<Task<IList<Item>>> OnRefresh { get; set; }
        [Parameter] public EventCallback<Item> OnItemEdited { get; set; }
        [Parameter] public EventCallback<Item> OnItemAdded { get; set; }
        [Parameter] public EventCallback<Item> OnItemDeleted { get; set; }
        [Parameter] public EventCallback<Item> OnItemSelected { get; set; }

        [Parameter] public Func<Item, bool> AllowDeleteExpression { get; set; }
        [Parameter] public int TotalCount { get; set; }

        public bool ShowSearch { get; set; } = true;
        protected IEnumerable<TableResult<object, Item>> TempItems { get; set; } = Enumerable.Empty<TableResult<object, Item>>();
        public List<IColumn<Item>> Columns { get; } = new List<IColumn<Item>>();
        public List<IColumn<Item>> VisibleColumns => Columns.Where(x => x.Visible).ToList();
        //public List<MenuDropdownItem<Item>> AllRowActions { get; set; } = new List<MenuDropdownItem<Item>>();
        public int PageNumber { get; set; }
        public int VisibleColumnCount => Columns.Count(x => x.Visible); // + (AllRowActions.Any() ? 1 : 0);
        public string SearchText { get; set; }
        public bool ResetPage { get; set; }
        public bool IsAddInProgress { get; set; }
        public bool ReloadingItems { get; set; }
        public Item CurrentEditItem { get; private set; }
        public Item SelectedItem { get; private set; }
        protected IDictionary<string, object> Attributes { get; set; }
        public bool ChangedItem { get; set; }
        public bool AllowAdd => OnItemAdded.HasDelegate;
        public bool AllowDelete => OnItemDeleted.HasDelegate;
        public bool AllowEdit => OnItemEdited.HasDelegate;
        public bool HasGrouping => Columns.Any(x => x.GroupBy);
        public TheGridDataFactory<Item> DataFactory { get; set; }

        protected async override Task OnParametersSetAsync()
        {
            await Update();
        }

        protected async override Task OnInitializedAsync()
        {
            DataFactory = new TheGridDataFactory<Item>(Columns, this);
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
            //AllRowActions = GetRowActions();
            await Update();
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

        public async Task OnSearchChanged(ChangeEventArgs args)
        {
            try
            {
                SearchText = args.Value.ToString();
                ResetPage = true;
                await Update();
                ResetPage = false;
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


            //ClassBuilder
            //.Add("page")
            //.Add(BackgroundColor.GetColorClass("bg"))
            //.Add(TextColor.GetColorClass("text"))
            //.ToString();

            return classBuileder
                .AddIf("grouped-table", HasGrouping)
                .ToString();
            //grouped-table

            //return new CssBuilder()
            //    .AddClass("")
            //    .AddClass("details-enabled", DetailsTemplate != null)
            //    .AddClass("details-enabled", OnItemSelected.HasDelegate)
            //    .AddClass("details-active", SelectedItem != null)
            //    .AddClass("grouped-table", HasGrouping)
            //    .Build();
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

        public bool IsSame(Item first, Item second)
        {
            return EqualityComparer<Item>.Default.Equals(first, second);
        }

        public bool IsEditingItem(Item item)
        {
            return CurrentEditItem != null && EqualityComparer<Item>.Default.Equals(item, CurrentEditItem);
        }

        protected bool ShowDetailsRow(Item item)
        {
            return DetailsTemplate != null && SelectedItem != null && IsSame(item, SelectedItem);
        }

        public async Task SetSelectedItem(Item item)
        {
            await OnItemSelected.InvokeAsync(item);
            if (SelectedItem != null && !IsSame(item, SelectedItem))
            {
                ChangedItem = true;
            }
            else if (SelectedItem != null && IsSame(item, SelectedItem))
            {
                SelectedItem = default;
                await Update();
                return;
            }

            SelectedItem = item;
            await Update();
        }

        public async Task ClearSelectedItem()
        {
            if (ChangedItem)
            {
                ChangedItem = false;
                return;
            }

            SelectedItem = default;
            await Update();
        }

        public async Task CloseEdit()
        {
            CurrentEditItem = default;
            IsAddInProgress = false;
            await Update();
        }

        public async Task Update()
        {
            if (CurrentEditItem == null || !TempItems.Any())
            {
                TempItems = DataFactory.GetData(Items, ResetPage);
               // AllRowActions = GetRowActions();
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
            PageNumber = (int)Math.Ceiling((decimal)TotalCount / PageSize) - 1;
            await Update();
        }

        public async Task Refresh()
        {
            await InvokeAsync(StateHasChanged);
        }

        //private List<MenuDropdownItem<Item>> GetRowActions()
        //{
        //    var result = new List<MenuDropdownItem<Item>>();
        //    result = result.Concat(RowActions).ToList();
        //    if (AllowEdit)
        //    {
        //        result.Add(new MenuDropdownItem<Item>
        //        {
        //            CallBack = EventCallback.Factory.Create<Item>(this, OnEditItem),
        //            Icon = "fe fe-edit-3",
        //            Title = L.GetString(x => x.FastEdit)
        //        });
        //    }

        //    if (AllowDelete)
        //    {
        //        result.Add(new MenuDropdownItem<Item>
        //        {
        //            CallBack = EventCallback.Factory.Create<Item>(this, OnDeleteItem),
        //            Icon = "fe fe-trash-2",
        //            Title = L.GetString(x => x.Delete),
        //            Id = "DeleteItem"
        //        });
        //    }

        //    return result;
        //}

        //public List<MenuDropdownItem<object>> GetHeaderActions()
        //{
        //    return HeaderActions;
        //}

        protected async Task OnAddItem()
        {
            if (IsAddInProgress)
            {
                return;
            }

            IsAddInProgress = true;

            Item tableItem = (Item)Activator.CreateInstance(typeof(Item));
            Items.Add(tableItem);

            await LastPage();
            OnEditItem(tableItem);
            await Update();
        }

        private async Task OnDeleteItem(Item item)
        {
            //var result = await AppService.ShowDialog("", title: L.GetString(x => x.DeleteConfirmItem), DialogType.Warning);
            //if (result.Ok)
            //{
            //    Items.Remove(item);
            //    await OnItemDeleted.InvokeAsync(item);
            //}

            await CloseEdit();
        }

        private void OnEditItem(Item tableItem)
        {
            CurrentEditItem = tableItem;
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
