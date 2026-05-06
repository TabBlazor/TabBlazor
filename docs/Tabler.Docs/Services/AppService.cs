using System;

namespace Tabler.Docs.Services;

public class AppService
{
    public Action OnSettingsUpdated;
    public AppSettings Settings { get; } = new();

    public void SettingsUpdated()
    {
        OnSettingsUpdated?.Invoke();
    }
}