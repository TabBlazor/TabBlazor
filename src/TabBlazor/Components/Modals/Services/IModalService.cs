using System;
using System.Threading.Tasks;

namespace TabBlazor.Services
{
    public interface IModalService
    {
        void SetTitle(string title);
        event Action<ModalResult> OnClose;
        Task<ModalResult> Show(string title, Type componentType, ModalParameters parameters = null, ModalOptions modalOptions = null);        
        void Cancel();
        void Close(ModalResult modalResult);
        void Close();
    }
}
