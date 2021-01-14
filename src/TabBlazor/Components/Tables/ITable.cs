using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TabBlazor.Components.Tables
{
    public interface ITable<TableItem>
    {
        int PageSize { get; }
        bool ShowFooter { get; set; }
        bool ShowSearch { get; set; }
        bool MultiSelect { get; set; }
        int PageNumber { get; }
        int TotalCount { get; }
        int VisibleColumnCount { get; }
        List<TableItem> SelectedItems { get; set; }
        IList<TableItem> Items { get; }
        RenderFragment<TableItem> RowActionTemplate { get; set; }
        bool ShowCheckboxes { get; set; }
        bool HasRowActions { get; }
         Task FirstPage();
        Task SetPage(int pageNumber);
        Task NextPage();
        Task PreviousPage();
        Task LastPage();
        Task ClearSelectedItem();
        List<IColumn<TableItem>> Columns { get; }
        List<IColumn<TableItem>> VisibleColumns { get; }
        void AddColumn(IColumn<TableItem> column);
        void RemoveColumn(IColumn<TableItem> column);
        Task RefreshItems(MouseEventArgs args);
        Task OnSearchChanged(ChangeEventArgs args);
        Task SelectAll();
        Task UnSelectAll();
        Task Update();
        void SetPageSize(int pageSize);
        string SearchText { get; set; }
        //List<MenuDropdownItem<TableItem>> AllRowActions { get; }
        string GetColumnWidth();
        Func<Task<IList<TableItem>>> OnRefresh { get; set; }
    }

    public interface ITableState
    {
        string SearchText { get; set; }
        int PageSize { get; set; }
        bool ShowFooter { get; set; }
        bool ShowSearch { get; set; }
        int PageNumber { get; set; }
        int TotalCount { get; set; }
        int VisibleColumnCount { get; }
    }

    public interface IInlineEditTable<TableItem>
    {
        List<IColumn<TableItem>> Columns { get; }
        List<IColumn<TableItem>> VisibleColumns { get; }
        bool IsAddInProgress { get; }
        bool ShowCheckboxes { get; }
        IList<TableItem> Items { get; }
        TableItem CurrentEditItem { get; }
        Task CloseEdit();
    }

    public interface ITableRow<TableItem>
    {
        List<IColumn<TableItem>> Columns { get; }
        List<IColumn<TableItem>> VisibleColumns { get; }
        IList<TableItem> Items { get; }
        TableItem SelectedItem { get; }
        List<TableItem> SelectedItems { get; }
        bool ShowCheckboxes { get;  }
        RenderFragment<TableItem> DetailsTemplate { get; }
        RenderFragment<TableItem> RowActionTemplate { get; set; }
        bool AllowEdit { get; }
        bool AllowDelete { get; }
        bool HasRowActions { get; }
        EventCallback<TableItem> OnItemSelected { get; }
        EventCallback<List<TableItem>> SelectedItemsChanged { get; }
        Task OnDeleteItem(TableItem item);
        void EditItem(TableItem item);
        Task SetSelectedItem(TableItem item);
    }

    public interface ITableRowActions<TableItem>
    {
        //List<MenuDropdownItem<TableItem>> AllRowActions { get; }
        Func<TableItem, bool> AllowDeleteExpression { get; }
        bool AllowDelete { get; }
        string GetColumnWidth();
    }

    public interface IDetailsTable<TableItem>
    {
        int VisibleColumnCount { get; }
        Task ClearSelectedItem();
        RenderFragment<TableItem> DetailsTemplate { get; }
    }
}