using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using TabBlazor.Components.Selects;

namespace TabBlazor
{
    /// <summary>
    /// A native single-select dropdown bound to a list of items, with optional clearing and custom text/value
    /// projection. <typeparamref name="TItem"/> is the source item type; <typeparamref name="TValue"/> is the
    /// selected value type. When the two differ, supply <see cref="ConvertExpression"/>.
    /// </summary>
    public partial class Select<TItem, TValue> : TablerBaseComponent
    {
        /// <summary>The items shown in the dropdown.</summary>
        [Parameter] public List<TItem> Items { get; set; }
        /// <summary>The currently selected value. Supports two-way binding via <c>@bind-SelectedValue</c>.</summary>
        [Parameter] public TValue SelectedValue { get; set; }
        /// <summary>Raised when the selected value changes.</summary>
        [Parameter] public EventCallback<TValue> SelectedValueChanged { get; set; }
        /// <summary>Raised after a selection change, in addition to <see cref="SelectedValueChanged"/>.</summary>
        [Parameter] public EventCallback Updated { get; set; }
        /// <summary>Projects an item to its display text. Defaults to <c>item.ToString()</c> when not set.</summary>
        [Parameter] public Func<TItem, string> TextExpression { get; set; }
        /// <summary>Projects an item to its bound value. Required when <typeparamref name="TItem"/> and <typeparamref name="TValue"/> differ.</summary>
        [Parameter] public Func<TItem, TValue> ConvertExpression { get; set; }
        /// <summary>Determines whether a given item is rendered as disabled (non-selectable).</summary>
        [Parameter] public Func<TItem, bool> DisabledExpression { get; set; }
        /// <summary>Text shown when <see cref="Items"/> is empty. Defaults to <c>"*No items*"</c>.</summary>
        [Parameter] public string ItemListEmptyText { get; set; } = "*No items*";
        /// <summary>Placeholder shown when nothing is selected. Defaults to <c>"*Select*"</c>.</summary>
        [Parameter] public string NoSelectedText { get; set; } = "*Select*";
        /// <summary>When true, shows a clear button to reset the selection. Defaults to false.</summary>
        [Parameter] public bool Clearable { get; set; }
        /// <summary>Controls the visual layout (e.g. flush). Defaults to <see cref="DisplayMode.Default"/>.</summary>
        [Parameter] public DisplayMode DisplayMode { get; set; } = DisplayMode.Default;

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
          .AddIf("form-control-flush", DisplayMode == DisplayMode.Flush)
          .ToString();

        private string InputGroupCss => new ClassBuilder()
          .AddIf("input-group input-group-flat", Clearable)
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
