using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using TabBlazor;
using Tabler.Docs.Services;

namespace Tabler.Docs.Components.General
{
    public partial class EditAppSettings : ComponentBase
    {
        [Inject] private AppService appService { get; set; }

       
        private EditContext editContext;
        private AppSettings settings => appService.Settings;
        protected override void OnInitialized()
        {
            
            editContext = new EditContext(settings);
            editContext.OnFieldChanged += OnFieldChanged;

            base.OnInitialized();
        }

        private void OnFieldChanged(object sender, FieldChangedEventArgs e)
        {
            appService.SettingsUpdated();
         
        }

        private void Update()
        {
           
            appService.SettingsUpdated();
        }
    }
}
