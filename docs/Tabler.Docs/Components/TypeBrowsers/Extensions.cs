using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tabler.Docs.Components.TypeBrowsers
{
    internal static class Extensions
    {
        internal static bool IsComponentParameter(this PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes().Any(e => e.GetType() == typeof(ParameterAttribute));
        }

     
        public static string GetFriendlyName(this Type type)
        {
            string friendlyName = type.Name;
            if (type.IsGenericType)
            {
                friendlyName = GetTypeString(type);
            }
            return friendlyName;
        }

        private static string GetTypeString(Type type)
        {
            var t = type.Name;
          
            var output = new StringBuilder();
            List<string> typeStrings = new List<string>();

            int iAssyBackTick = t.IndexOf('`') + 1;
            output.Append(t.Substring(0, iAssyBackTick - 1).Replace("[", string.Empty));
            var genericTypes = type.GetGenericArguments();

            foreach (var genType in genericTypes)
            {
                typeStrings.Add(genType.GetFriendlyName());
            }

            output.Append($"<{string.Join(", ", typeStrings)}>");
            return output.ToString();
        }

    }
}
