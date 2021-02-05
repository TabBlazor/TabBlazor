using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TabBlazor.Services
{
    public interface IModalService
    {
        void SetTitle(string title);
        event Action<ModalResult> OnClose;
        event Action Changed;
        IEnumerable<ModalModel> Modals { get; }
        Task<ModalResult> Show(string title, Type componentType, ModalParameters parameters = null, ModalOptions modalOptions = null);        
        void Close(ModalResult modalResult);
        void Close();
    }
}
