using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace TabBlazor.Components.Tables
{
    public class EditRowBase<TableItem> : TableRowComponentBase<TableItem>
    {
        [Parameter] public IInlineEditTable<TableItem> InlineEditTable { get; set; }
        [Parameter] public TableItem Item { get; set; }
            
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

        }

        public async Task OnEditItemCanceled()
        {
            if (InlineEditTable.IsAddInProgress)
            {
                InlineEditTable.Items.Remove(InlineEditTable.CurrentEditItem);
            }

            await InlineEditTable.CloseEdit();
        }



    }
}
