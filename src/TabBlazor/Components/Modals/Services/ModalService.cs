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
     
        internal event Action<string, ModalOptions, RenderFragment, ModalParameters> OnShow;
        public event Action OnChanged;

        private Stack<ModalModel> modals = new Stack<ModalModel>();
        internal ModalModel modalModel;

        public IEnumerable<ModalModel> Modals { get { return modals; } }

        //public void SetTitle(string title)
        //{
        //    OnTitleSet?.Invoke(title);
        //}

        public Task<ModalResult> ShowAsync(string title, Type componentType, ModalParameters parameters, ModalOptions modalOptions = null)
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
            OnChanged?.Invoke();
            return modalModel.Task;

        }

        public void Close(ModalResult modalResult)
        {
            ModalModel modalToClose = modals.Pop();
            modalToClose.TaskSource.SetResult(modalResult);
            OnChanged?.Invoke();

        }

        public void Close()
        {
            Close(ModalResult.Cancel());
        }
    }
}
