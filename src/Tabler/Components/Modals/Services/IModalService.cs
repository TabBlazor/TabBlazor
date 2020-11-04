using System;
using System.Threading.Tasks;

namespace Tabler.Components
{
    public interface IModalService
    {
        void SetTitle(string title);
        event Action<ModalResult> OnClose;
        Task<ModalResult> Show(string title, Type componentType, ModalParameters parameters, ModalSize modalSize = ModalSize.Large);        
        void Cancel();
        void Close(ModalResult modalResult);
        void Close();
    }
}
