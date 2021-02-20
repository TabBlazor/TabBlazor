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

        private Dictionary<IColumn<TableItem>, object> orgValues = new Dictionary<IColumn<TableItem>, object>();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            foreach (var column in InlineEditTable.Columns.Where(e => e.Property != null && e.EditorTemplate != null))
            {
                orgValues.Add(column, column.GetValue(Item));
            }


        }

        public async Task OnEditItemCanceled()
        {
            //TODO: We need to spend some on this..
            //foreach (var orgValue in orgValues)
            //{
            //    var newValue = orgValue.Key.GetValue(Item);
            //    if (newValue != orgValue.Value)
            //    {
            //        var propInfo = (PropertyInfo)orgValue.Key.Property?.GetPropertyMemberInfo();
            //        // propInfo.SetValue(Item, orgValue.Value);
            //        //SetDeepValue(Item, orgValue.Key.Property, orgValue.Value);
            //    }
            //}

            if (InlineEditTable.IsAddInProgress)
            {
                InlineEditTable.Items.Remove(InlineEditTable.CurrentEditItem);
            }

            await InlineEditTable.CloseEdit();
        }


        public static void SetDeepValue<TObject, T>(TObject target, Expression<Func<TObject, T>> propertyToSet, T valueToSet)
        {
            List<MemberInfo> members = new List<MemberInfo>();

            Expression exp = propertyToSet.Body;

            // There is a chain of getters in propertyToSet, with at the 
            // beginning a ParameterExpression. We put the MemberInfo of
            // these getters in members. We don't really need the 
            // ParameterExpression

            while (exp != null)
            {
                MemberExpression mi = exp as MemberExpression;

                if (mi != null)
                {
                    members.Add(mi.Member);
                    exp = mi.Expression;
                }
                else
                {
                    ParameterExpression pe = exp as ParameterExpression;

                    if (pe == null)
                    {
                        // We support only a ParameterExpression at the base
                        //throw new NotSupportedException();
                        // members.Add()
                    }

                    break;
                }
            }

            if (members.Count == 0)
            {
                // We need at least a getter
                throw new NotSupportedException();
            }

            // Now we must walk the getters (excluding the last).
            object targetObject = target;

            // We have to walk the getters from last (most inner) to second
            // (the first one is the one we have to use as a setter)
            for (int i = members.Count - 1; i >= 1; i--)
            {
                PropertyInfo pi = members[i] as PropertyInfo;

                if (pi != null)
                {
                    targetObject = pi.GetValue(targetObject);
                }
                else
                {
                    FieldInfo fi = (FieldInfo)members[i];
                    targetObject = fi.GetValue(targetObject);
                }
            }

            // The first one is the getter we treat as a setter
            {
                PropertyInfo pi = members[0] as PropertyInfo;

                if (pi != null)
                {
                    pi.SetValue(targetObject, valueToSet);
                }
                else
                {
                    FieldInfo fi = (FieldInfo)members[0];
                    fi.SetValue(targetObject, valueToSet);
                }
            }
        }

    }
}
