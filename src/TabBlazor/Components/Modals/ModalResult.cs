using System;

namespace TabBlazor
{
    public class ModalResult
    {
        public object Data { get; }
        public Type DataType { get; }
        public bool Cancelled { get; }

        internal ModalResult(object data, Type resultType, bool cancelled)
        {
            Data = data;
            DataType = resultType;
            Cancelled = cancelled;
        }

        public static ModalResult Ok() => new ModalResult(default, typeof(object),false);
        public static ModalResult Ok<T>(T result) => new ModalResult(result, typeof(T), false);

        public static ModalResult Cancel() => new ModalResult(default, typeof(object), true);
    }
}
