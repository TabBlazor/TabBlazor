using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor.Services
{
    public class ModalService : IModalService
    {
        public event Action<ModalResult> OnClose;
        internal event Action<string, ModalOptions, RenderFragment, ModalParameters> OnShow;
        internal event Action<string> OnTitleSet;
        public event Action Changed;

        private Stack<ModalModel> modals = new Stack<ModalModel>();
        internal ModalModel modalModel;

        public IEnumerable<ModalModel> Modals { get { return modals; } }

        public void SetTitle(string title)
        {
            OnTitleSet?.Invoke(title);
        }

        public Task<ModalResult> Show(string title, Type componentType, ModalParameters parameters, ModalOptions modalOptions = null)
        {
            modalModel = new ModalModel(componentType, title, parameters, modalOptions);
            if (!typeof(ComponentBase).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"{componentType.FullName} must be a Blazor Component");
            }

            var content = new RenderFragment(x =>
            {
                x.OpenComponent(1, componentType);

                if (parameters != null)
                {
                    var i = 1;
                    foreach (var parameter in parameters)
                    {
                        x.AddAttribute(i, parameter.Key, parameter.Value);
                        i++;
                    }
                }

                x.CloseComponent();
            });

            if (modalOptions == null)
            {
                modalOptions = new ModalOptions();
            }

            OnShow?.Invoke(title, modalOptions, content, parameters);

            modals.Push(modalModel);
            Changed?.Invoke();
            return modalModel.Task;

        }



        public void Cancel()
        {
            OnClose?.Invoke(ModalResult.Cancel());
            modalModel.TaskSource.SetResult(ModalResult.Cancel());
        }

        public void Close(ModalResult modalResult)
        {
            // OnClose?.Invoke(modalResult);
            // modalModel.TaskSource.SetResult(modalResult);
            ModalModel modalToClose = modals.Pop();
            modalToClose.TaskSource.SetResult(modalResult);
            Changed?.Invoke();

        }

        public void Close()
        {
            Close(ModalResult.Cancel());
        }
    }
}
