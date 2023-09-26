using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using System.Reflection;
using System.Collections;
using System.Threading.Tasks;
using TabBlazor.Services;

namespace TabBlazor
{
    public partial class ObjectBrowser
    {
        [Inject] public IModalService ModalService { get; set; }
        [Parameter] public object Object { get; set; }

        private Type objectType;
        private List<PropertyInfo> properties = new();
        private bool isEnumerable;

        private ObjectItem objectItem;
        private List<ObjectItem> listItems = new();


        protected override void OnInitialized()
        {
            if (Object == null)
            {
                return;
            }

            isEnumerable = IsEnumerable(Object.GetType());
            objectType = GetAnyElementType(Object.GetType());
            properties = objectType.GetProperties().ToList();

            if (isEnumerable)
            {
                foreach (var item in (IEnumerable)Object)
                {
                    listItems.Add(new ObjectItem(item, properties));
                }
            }
            else
            {
                objectItem = new ObjectItem(Object, properties);
            }
        }

        private bool SearchObject(ObjectItem objectItem, string searchText)
        {
            return objectItem.SearchValues(searchText);
        }

        private bool IsEnumerable(Type type)
        {
            if (typeof(string) == type) { return false; }
            return typeof(IEnumerable).IsAssignableFrom(type);
        }

        private static Type GetAnyElementType(Type type)
        {
            // BadgeType is Array
            // short-circuit if you expect lots of arrays 
            if (type.IsArray)
                return type.GetElementType();

            // type is IEnumerable<T>;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            // type implements/extends IEnumerable<T>;
            var enumType = type.GetInterfaces()
                                    .Where(t => t.IsGenericType &&
                                           t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                    .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();
            return enumType ?? type;
        }

        private async Task ObjectDetails(object myObject)
        {
            var component = new RenderComponent<ObjectBrowser>().Set(e => e.Object, myObject);
            var result = await ModalService.ShowAsync(myObject.GetType().Name, component, new ModalOptions { Size = ModalSize.XLarge });
        }
    }


}