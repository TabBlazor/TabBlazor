using System;

namespace TabBlazor
{
    public class OffcanvasResult
    {
        public object Data { get; }
        public Type DataType { get; }
        public bool Cancelled { get; }

        internal OffcanvasResult(object data, Type resultType, bool cancelled)
        {
            Data = data;
            DataType = resultType;
            Cancelled = cancelled;
        }

        public static OffcanvasResult Ok() => new OffcanvasResult(default, typeof(object),false);
        public static OffcanvasResult Ok<T>(T result) => new OffcanvasResult(result, typeof(T), false);

        public static OffcanvasResult Cancel() => new OffcanvasResult(default, typeof(object), true);
    }
}
