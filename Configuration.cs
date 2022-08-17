using System;
using Dalamud.Configuration;

namespace AetherCurrentHelper;

[Serializable]
internal partial class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool ShowFinished { get; set; } = true;
    public bool OnlyShowCurrentZone { get; set; }
}

internal partial class Configuration : IDisposable
{
    private static Configuration instance = null!;
    public static Configuration Instance => instance ??= Load();

    internal static Configuration Load()
    {
        return instance = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    }

    internal static void Save()
    {
        Service.PluginInterface.SavePluginConfig(instance);
    }

    void IDisposable.Dispose()
    {
        instance = null!;
    }
}
