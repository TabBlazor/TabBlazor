using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tabler.Components.Selects;

namespace Tabler.Components
{
    public partial class ItemSelect<TValue> : TablerBaseComponent
    {
        [Parameter] public List<TValue> Items { get; set; }
        [Parameter] public TValue SelectedValue { get; set; }
        [Parameter] public EventCallback<TValue> SelectedValueChanged { get; set; }
        [Parameter] public EventCallback Updated { get; set; }
        [Parameter] public Func<TValue, string> TextExpression { get; set; }
        [Parameter] public Func<TValue, string> ValueExpression { get; set; }
        [Parameter] public string ItemListEmptyText { get; set; } = "*No items*";
        [Parameter] public string NoSelectedText { get; set; } = "*Select*";
        [Parameter] public bool IsClearable { get; set; }
             
        protected List<ListItem<TValue>> itemList = new List<ListItem<TValue>>();

        protected override void OnParametersSet()
        {
            PopulateItemList();
            //CssClass = $"{CssClass} {SetBorderCss()}";
        }

        protected override string ClassNames => ClassBuilder
          .Add("form-select")
          .ToString();

        //private string SetBorderCss()
        //{
        //    if (IsClearable && IsBorderless)
        //        return "border-0 p-1";

        //    if (IsClearable)
        //        return "border-right-0";

        //    if (IsBorderless)
        //        return "border-0 p-1";

        //    return string.Empty;
        //}

        protected bool ItemNotInList()
        {
            if (SelectedValue == null) return true;
            foreach (var item in itemList)
            {
                if (item.Value == GetValue(SelectedValue)) return false;
            }
            return true;
        }

        private void PopulateItemList()
        {
            itemList = new List<ListItem<TValue>>();

            if (Items != null)
            {
                foreach (var item in Items)
                {
                    var listItem = new ListItem<TValue>
                    {
                        Text = GetText(item),
                        Value = GetValue(item),
                        Item = item
                    };
                    itemList.Add(listItem);
                }
            }

        }

        protected void ValueChanged(ChangeEventArgs e)
        {
            var value = e.Value.ToString();
            var listItem = itemList.FirstOrDefault(v => v.Value == value);

            if (listItem != null)
            {
                SelectedValue = listItem.Item;
            }
            else
            {
                SelectedValue = default;
            }

            SelectedValueChanged.InvokeAsync(SelectedValue);
            Updated.InvokeAsync(null);
        }

        protected string GetValue(TValue item)
        {
            if (ValueExpression == null) return null;

            return ValueExpression.Invoke(item);
        }

        private string GetText(TValue item)
        {
            if (TextExpression == null) return "No Expresssion Set up!";
            return TextExpression.Invoke(item);
        }

        protected async void Clear()
        {
            SelectedValue = default;
            await SelectedValueChanged.InvokeAsync(SelectedValue);
        }

    }
}
