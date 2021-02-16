using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TabBlazor.Services
{
    public interface IModalService
    {
        event Action OnChanged;
        IEnumerable<ModalModel> Modals { get; }
        Task<ModalResult> ShowAsync(string title, DynamicComponent component, ModalOptions modalOptions = null);        
        void Close(ModalResult modalResult);
        void Close();
    }
}
