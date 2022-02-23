using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace TabBlazor
{
    public class ModalModel
    {
        public ModalModel(RenderFragment contents, string title, ModalOptions options)
        {
            TaskSource = new TaskCompletionSource<ModalResult>();
            ModalContents = contents;
            Title = title;
            Options = options ?? new ModalOptions();
        }

   
        internal TaskCompletionSource<ModalResult> TaskSource { get; }

        public Task<ModalResult> Task { get { return TaskSource.Task; } }
        public string Title { get; set; }
     
       public RenderFragment ModalContents { get; private set; }
        public ModalOptions Options { get; }

       
        
    }
}
