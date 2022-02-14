using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IconGenerator
{
    public static class Helpers
    {
        public static string GetStaticPropertyString(string iconName, string elementsString)
        {
            return $"public static string {iconName} => @\"{elementsString}\";";
        }

    }
}
