using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tabler.Components
{
    public class ModalService : IModalService
    {
        public event Action<ModalResult> OnClose;
        internal event Action<string, string, RenderFragment, ModalParameters> OnShow;
        internal event Action<string> OnTitleSet;

        internal ModalModel modalModel;

        public void SetTitle(string title)
        {
            OnTitleSet?.Invoke(title);
        }

        public Task<ModalResult> Show(string title, Type componentType, ModalParameters parameters, ModalSize modalSize = ModalSize.Large)
        {

            modalModel = new ModalModel(componentType, title, parameters, new ModalOptions());

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

            OnShow?.Invoke(title, GetehaderClass(modalSize), content, parameters);
            return modalModel.Task;
        }

        private string GetehaderClass(ModalSize modalSize)
        {
            return $"modal-size-{((int)modalSize).ToString()}";
        }

        public void Cancel()
        {
            OnClose?.Invoke(ModalResult.Cancel());
            modalModel.TaskSource.SetResult(ModalResult.Cancel());
        }

        public void Close(ModalResult modalResult)
        {
            OnClose?.Invoke(modalResult);
            modalModel.TaskSource.SetResult(modalResult);
        }

        public void Close()
        {
            OnClose?.Invoke(ModalResult.Cancel());
            modalModel.TaskSource.SetResult(ModalResult.Cancel());
        }
    }
}
