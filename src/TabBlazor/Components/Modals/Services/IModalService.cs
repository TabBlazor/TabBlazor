using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TabBlazor.Components.Modals;

namespace TabBlazor.Services
{
    /// <summary>
    /// Service for showing modals and dialogs. Registered by <c>AddTabBlazor</c>; inject it and call
    /// <see cref="ShowAsync{TComponent}"/> to display a component as a modal, awaiting the user's result.
    /// </summary>
    public interface IModalService
    {
        /// <summary>Raised when the set of open modals changes.</summary>
        event Action OnChanged;
        /// <summary>The currently open modals.</summary>
        IEnumerable<ModalModel> Modals { get; }
        /// <summary>
        /// Shows <typeparamref name="TComponent"/> as a modal and awaits its result. Use
        /// <see cref="RenderComponent{T}"/> to pass parameters; close from within via <see cref="Close(ModalResult)"/>.
        /// </summary>
        /// <param name="title">The modal title.</param>
        /// <param name="component">The component (with parameters) to render inside the modal.</param>
        /// <param name="modalOptions">Optional appearance/behavior options.</param>
        /// <returns>The result, including whether it was cancelled and any returned data.</returns>
        Task<ModalResult> ShowAsync<TComponent>(string title, RenderComponent<TComponent> component, ModalOptions modalOptions = null) where TComponent : IComponent;
        /// <summary>Closes the top-most modal with the given result.</summary>
        void Close(ModalResult modalResult);
        /// <summary>Closes the top-most modal as cancelled.</summary>
        void Close();
        /// <summary>Shows a simple confirm/alert dialog and returns whether the user confirmed.</summary>
        Task<bool> ShowDialogAsync(DialogOptions options);

        /// <summary>Updates the title of the top-most modal.</summary>
        void UpdateTitle(string title);
        /// <summary>Forces open modals to re-render.</summary>
        void Refresh();

        /// <summary>Registers a <see cref="ModalView"/> host. Called internally by the component.</summary>
        ModalViewSettings RegisterModalView(ModalView modalView);
        /// <summary>Unregisters a <see cref="ModalView"/> host. Called internally by the component.</summary>
        void UnRegisterModalView(ModalView modalView);
        //int AddZIndex();
        //int DeductZIndex();

        //int AddTopOffset();
        //int DeductTopOffset();

    }
}
