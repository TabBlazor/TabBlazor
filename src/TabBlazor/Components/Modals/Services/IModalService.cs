using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TabBlazor.Components.Modals.Standard;

namespace TabBlazor.Services
{
    public interface IModalService
    {
        event Action OnChanged;
        IEnumerable<ModalModel> Modals { get; }
        Task<ModalResult> ShowAsync<TComponent>(string title, RenderComponent<TComponent> component, ModalOptions modalOptions = null) where TComponent : IComponent;        
        void Close(ModalResult modalResult);
        void Close();
        Task<bool> ShowConfirmDialogAsync(ConfirmOptions options);
    }
}
