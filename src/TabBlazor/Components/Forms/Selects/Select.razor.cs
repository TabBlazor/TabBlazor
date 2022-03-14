using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using TabBlazor.Components.Selects;

namespace TabBlazor
{
    public partial class Select<TItem, TValue> : TablerBaseComponent
    {
        [Parameter] public List<TItem> Items { get; set; }
        [Parameter] public TValue SelectedValue { get; set; }
        [Parameter] public EventCallback<TValue> SelectedValueChanged { get; set; }
        [Parameter] public EventCallback Updated { get; set; }
        [Parameter] public Func<TItem, string> TextExpression { get; set; }
        [Parameter] public Func<TItem, TValue> ConvertExpression { get; set; }
        [Parameter] public Func<TItem, bool> DisabledExpression { get; set; }
        [Parameter] public string ItemListEmptyText { get; set; } = "*No items*";
        [Parameter] public string NoSelectedText { get; set; } = "*Select*";
        [Parameter] public bool Clearable { get; set; }

        protected List<ListItem<TItem, TValue>> itemList = new();

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
            PopulateItemList();
        }

        protected override string ClassNames => ClassBuilder
          .Add("form-control form-select")
          .ToString();

        protected bool IsSelected(TValue value)
        {
            return (EqualityComparer<TValue>.Default.Equals(SelectedValue, value));
        }

        protected bool ItemNotInList()
        {
            if (SelectedValue == null) return true;
            foreach (var item in itemList)
            {
                if (IsSelected(item.Value)) return false;
            }
            return true;
        }

        private void PopulateItemList()
        {
            itemList = new List<ListItem<TItem, TValue>>();

            if (Items != null)
            {
                foreach (var item in Items)
                {
                    var listItem = new ListItem<TItem, TValue>
                    {
                        Text = GetText(item),
                        Value = GetValue(item),
                        Item = item
                    };

                    if (DisabledExpression != null)
                    {
                        listItem.Disabled = DisabledExpression(item);
                    }

                    itemList.Add(listItem);
                }
            }

        }

        protected void ValueChanged(ChangeEventArgs e)
        {
            var id = e.Value.ToString();
            var listItem = itemList.FirstOrDefault(v => v.Id == id);

            if (listItem != null)
            {
                SelectedValue = listItem.Value;
            }
            else
            {
                SelectedValue = default;
            }

            SelectedValueChanged.InvokeAsync(SelectedValue);
            Updated.InvokeAsync(null);
        }

        protected TValue GetValue(TItem item)
        {
            if (ConvertExpression == null) return default;

            return ConvertExpression.Invoke(item);
        }

        private string GetText(TItem item)
        {
            if (TextExpression == null) { return item.ToString(); }
            return TextExpression.Invoke(item);
        }

        protected async void Clear()
        {
            SelectedValue = default;
            await SelectedValueChanged.InvokeAsync(SelectedValue);
        }

    }
}
