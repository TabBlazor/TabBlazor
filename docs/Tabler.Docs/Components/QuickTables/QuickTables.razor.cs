using System;
using Microsoft.AspNetCore.Components;

namespace Tabler.Docs.Components.QuickTables;

public partial class QuickTables
{
    [Inject]public IServiceProvider Provider { get; set; }
    
    protected override void OnInitialized()
    {
        SeedData.EnsureSeeded(Provider);
    }
}