using System;
using Microsoft.AspNetCore.Components;

namespace Tabler.Docs.Components.DataViews;

public partial class DataViews
{
    [Inject] public IServiceProvider Provider { get; set; }

    protected override void OnInitialized()
    {
        SeedData.EnsureSeeded(Provider);
    }
}
