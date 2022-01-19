using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TabBlazor.Components.Modals;


namespace TabBlazor.Services
{
    public class ModalService : IModalService, IDisposable
    {

        public ModalService(NavigationManager navigationManager)
        {
            this.navigationManager = navigationManager;
            this.navigationManager.LocationChanged += LocationChanged;
        }


        private int zIndex = 1200;
        private const int zIndexIncrement = 10;
        private int topOffset;
        private const int topOffsetIncrement = 20;

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

        public async Task<bool> ShowDialogAsync(DialogOptions options)
        {
            var component = new RenderComponent<DialogModal>().
                Set(e=> e.Options, options);
            var result = await ShowAsync("", component, new ModalOptions { Size = ModalSize.Small, ShowHeader = false, StatusColor = options.StatusColor });
            return !result.Cancelled;
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

        public void UpdateTitle(string title)
        {
            var modal = Modals.LastOrDefault();
            if (modal != null)
            {
                modal.Title = title;
                OnChanged?.Invoke();
            }
        }

        public void Refresh()
        {
            var modal = Modals.LastOrDefault();
            if (modal != null)
            {
                OnChanged?.Invoke();
            }
        }

        public ModalViewSettings RegisterModalView(ModalView modalView)
        {
            var settings = new ModalViewSettings { TopOffset = topOffset, ZIndex = zIndex };
            zIndex += zIndexIncrement;
            topOffset += topOffsetIncrement;

            return settings;
        }

        public void UnRegisterModalView(ModalView modalView)
        {
            zIndex -= zIndexIncrement;
            topOffset -= topOffsetIncrement;
        }

        //public int AddZIndex()
        //{
        //    zIndex += zIndexIncrement;
        //    return zIndex;
        //}

        //public int DeductZIndex()
        //{
        //    zIndex -= zIndexIncrement;
        //    return zIndex;
        //}

        //public int AddTopOffset()
        //{
        //    var offset = yOffset;
        //    yOffset += yOffsetIncrement;
        //    return offset;
        //}

        //public int DeductTopOffset()
        //{
        //    yOffset -= yOffsetIncrement;
        //    return yOffset;
        //}
    }
}
