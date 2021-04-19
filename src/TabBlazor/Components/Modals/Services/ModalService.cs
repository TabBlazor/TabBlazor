using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor.Services
{
    public class ModalService : IModalService, IDisposable
    {

        public ModalService(NavigationManager navigationManager)
        {
            this.navigationManager = navigationManager;
            this.navigationManager.LocationChanged += LocationChanged;
        }

   
        public event Action OnChanged;

        private Stack<ModalModel> modals = new Stack<ModalModel>();
        internal ModalModel modalModel;
        private readonly NavigationManager navigationManager;

        public IEnumerable<ModalModel> Modals { get { return modals; } }

        public Task<ModalResult> ShowAsync<TComponent>(string title, RenderComponent<TComponent> component, ModalOptions modalOptions = null) where TComponent : IComponent
        {
            modalModel = new ModalModel(component.Contents, title, modalOptions);
            modals.Push(modalModel);
            OnChanged?.Invoke();
            return modalModel.Task;

        }

        private void LocationChanged(object sender, LocationChangedEventArgs e)
        {
            CloseAll();
        }

        private void CloseAll()
        {
            foreach (var x in modals.ToList())
            {
                Close();
            }
        }

        public void Close(ModalResult modalResult)
        {
            if (modals.Any())
            {
                ModalModel modalToClose = modals.Pop();
                modalToClose.TaskSource.SetResult(modalResult);
            }
            
            OnChanged?.Invoke();
        }

        public void Close()
        {
            Close(ModalResult.Cancel());
        }

        public void Dispose()
        {
            navigationManager.LocationChanged -= LocationChanged;
        }
    }
}
