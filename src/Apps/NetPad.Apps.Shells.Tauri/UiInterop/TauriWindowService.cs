using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;
using NetPad.Scripts;

namespace NetPad.Apps.Shells.Tauri.UiInterop;

public class TauriWindowService(IIpcService ipcService) : IUiWindowService
{
    public Task OpenMainWindowAsync()
    {
        // Tauri opens the main window automatically
        return Task.CompletedTask;
    }

    public async Task OpenSettingsWindowAsync(string? tab = null)
    {
        var command = new OpenWindowCommand(WindowIds.Settings);
        command.Options.Height = 0.67;
        command.Options.Width = 0.5;

        if (tab != null) command.Metadata.Add("tab", tab);

        await ipcService.SendAsync(command);
    }

    public async Task OpenScriptConfigWindowAsync(Script script, string? tab = null)
    {
        var command = new OpenWindowCommand(WindowIds.ScriptConfig);
        command.Options.Height = 0.67;
        command.Options.Width = 0.8;

        command.Metadata.Add("script-id", script.Id);
        if (tab != null) command.Metadata.Add("tab", tab);

        await ipcService.SendAsync(command);
    }

    public async Task OpenDataConnectionWindowAsync(Guid? dataConnectionId, bool copy = false)
    {
        if (copy && dataConnectionId == null)
        {
            throw new ArgumentException("Data connection id must be provided when copying a connection.");
        }

        var command = new OpenWindowCommand(WindowIds.DataConnection);
        command.Options.Height = 0.4;
        command.Options.Width = 0.5;

        if (dataConnectionId != null)
        {
            command.Metadata.Add("data-connection-id", dataConnectionId);
        }

        if (copy)
        {
            command.Metadata.Add("copy", "true");
        }

        await ipcService.SendAsync(command);
    }

    public async Task OpenOutputWindowAsync()
    {
        var command = new OpenWindowCommand(WindowIds.Output);
        command.Options.Height = 0.67;
        command.Options.Width = 0.8;

        await ipcService.SendAsync(command);
    }

    public async Task OpenCodeWindowAsync()
    {
        var command = new OpenWindowCommand(WindowIds.Code);
        command.Options.Height = 0.67;
        command.Options.Width = 0.8;

        await ipcService.SendAsync(command);
    }
}
