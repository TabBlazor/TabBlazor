using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace TabBlazor
{
    public class ModalModel
    {
        public ModalModel(DynamicComponent component, string title, ModalOptions options)
        {
            TaskSource = new TaskCompletionSource<ModalResult>();
            Component = component;
            Title = title;
            Options = options ?? new ModalOptions();
        }

   
        internal TaskCompletionSource<ModalResult> TaskSource { get; }

        public Task<ModalResult> Task { get { return TaskSource.Task; } }
        public string Title { get; }
        private DynamicComponent Component { get; set; }
       
        public ModalOptions Options { get; }

        public RenderFragment ModalContents => Component.Contents;
        
    }
}
