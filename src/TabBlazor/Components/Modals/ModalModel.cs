using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace TabBlazor
{
    public class ModalModel
    {
        public ModalModel(Type dialogComponentType, string title, ModalParameters parameters, ModalOptions options)
        {
            TaskSource = new TaskCompletionSource<ModalResult>();
            DialogComponentType = dialogComponentType;
            Title = title;
            Parameters = parameters;
            Options = options;
        }

        private Type DialogComponentType { get; }
        internal TaskCompletionSource<ModalResult> TaskSource { get; }

        public Task<ModalResult> Task { get { return TaskSource.Task; } }
        public string Title { get; }
        public ModalParameters Parameters { get; }
        public ModalOptions Options { get; }

        public RenderFragment DialogContents
        {
            get
            {
                RenderFragment content = new RenderFragment(x =>
                {
                    int seq = 1;
                    x.OpenComponent(seq++, DialogComponentType);
                    foreach (var parameter in Parameters)
                        x.AddAttribute(seq++, parameter.Key, parameter.Value);
                    x.CloseComponent();
                });
                return content;
            }
        }
    }
}
