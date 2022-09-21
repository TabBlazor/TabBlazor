using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TabBlazor
{
    internal class ObjectItem
    {
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();
        private List<PropertyInfo> properties = new List<PropertyInfo>();
      
        public ObjectItem(object myObject, IEnumerable<PropertyInfo> properties)
        {
            this.Object = myObject;
            this.properties = properties.ToList(); 
        }

     
        public object Object { get; set; }

        public List<PropertyInfo> Properties => properties;

        public bool SearchValues(string searchText)
        {
            foreach (var prop in properties)
            {
                var value = GetPropertyValue(prop)?.ToString();

                if (value != null && value.Contains(searchText, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public object GetPropertyValue(string propertyName)
        {
            if (values.ContainsKey(propertyName))
            {
                return values[propertyName];
            }

            var prop = properties.FirstOrDefault(e => e.Name == propertyName);
            if (prop == null) { throw new Exception($"Unable to look up property {propertyName}"); }

            return GetPropertyValue(prop);
        }

        public object GetPropertyValue(PropertyInfo property)
        {
            if (values.ContainsKey(property.Name))
            {
                return values[property.Name];
            }

            if (Object == null) { return null; }

            var propValue = property.GetValue(Object, null);
            values.Add(property.Name, propValue);

            return propValue;
        }

    }


}