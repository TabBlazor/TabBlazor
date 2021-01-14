using System;
using System.Collections.Generic;
using System.Linq;

namespace TabBlazor
{
    public static class EnumInfo
    {
        public static List<TEnum> GetList<TEnum>()
           where TEnum : struct
        {
            if (!typeof(TEnum).IsEnum) throw new InvalidOperationException();
            return Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToList();
        }

    }
}
