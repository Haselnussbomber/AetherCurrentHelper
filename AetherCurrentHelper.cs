using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;

namespace AetherCurrentHelper;

public class AetherCurrentHelper : IDalamudPlugin, IDisposable
{
    public string Name => "Aether Current Helper";

    private readonly WindowSystem WindowSystem = new("AetherCurrentHelper");
    private readonly PluginWindow PluginWindow;

    public AetherCurrentHelper(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        Service.GameFunctions = new();

        Configuration.Load();

        PluginWindow = new PluginWindow();
        WindowSystem.AddWindow(PluginWindow);

        var commandInfo = new CommandInfo(OnCommand)
        {
            HelpMessage = "Show Window"
        };

        Service.Commands.AddHandler("/aether", commandInfo);
        Service.Commands.AddHandler("/aethercurrents", commandInfo);
        Service.Commands.AddHandler("/aethercurrenthelper", commandInfo);

        Service.PluginInterface.UiBuilder.Draw += OnDraw;
        Service.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
    }

    private void OnDraw()
    {
        try
        {
            WindowSystem.Draw();
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Unexpected exception in OnDraw");
        }
    }

    private void OnCommand(string command, string args)
    {
        PluginWindow.Toggle();
    }

    private void OnOpenConfigUi()
    {
        PluginWindow.Toggle();
    }

    void IDisposable.Dispose()
    {
        Service.PluginInterface.UiBuilder.Draw -= OnDraw;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;

        Service.Commands.RemoveHandler("/aether");
        Service.Commands.RemoveHandler("/aethercurrents");
        Service.Commands.RemoveHandler("/aethercurrenthelper");

        WindowSystem.RemoveAllWindows();
    }
}
