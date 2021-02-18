using System;
using System.Collections.Generic;
using System.Linq;

namespace TabBlazor
{
    public static class EnumHelper
    {
        public static List<TEnum> GetList<TEnum>() where TEnum : struct, Enum
        {
            if (!typeof(TEnum).IsEnum) throw new InvalidOperationException();
            return Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToList();
        }

        public static List<TEnum?> GetNullableList<TEnum>() where TEnum : struct, Enum
        {
            return Enum.GetValues(Nullable.GetUnderlyingType(typeof(TEnum)) ?? typeof(TEnum)).Cast<TEnum?>().ToList();
        }
    }
}
