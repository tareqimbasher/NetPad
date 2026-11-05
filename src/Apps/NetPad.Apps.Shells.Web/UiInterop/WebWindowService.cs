using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;
using NetPad.Scripts;

namespace NetPad.Apps.Shells.Web.UiInterop;

public class WebWindowService(IIpcService ipcService) : IUiWindowService
{
    public Task OpenMainWindowAsync()
    {
        throw new PlatformNotSupportedException();
    }

    public async Task OpenSettingsWindowAsync(string? tab = null)
    {
        var command = new OpenWindowCommand(WindowIds.Settings);
        command.Options.Height = WindowSizes.SettingsHeight;
        command.Options.Width = WindowSizes.SettingsWidth;

        if (tab != null) command.Metadata.Add("tab", tab);

        await ipcService.SendAsync(command);
    }

    public async Task OpenScriptConfigWindowAsync(Script script, string? tab = null)
    {
        var command = new OpenWindowCommand(WindowIds.ScriptConfig);
        command.Options.Height = WindowSizes.ScriptConfigHeight;
        command.Options.Width = WindowSizes.ScriptConfigWidth;

        command.Metadata.Add("script-id", script.Id);
        if (tab != null) command.Metadata.Add("tab", tab);

        await ipcService.SendAsync(command);
    }

    public async Task OpenDataConnectionWindowAsync(Guid? dataConnectionId, bool copy = false, bool isServer = false)
    {
        if (copy && dataConnectionId == null)
        {
            throw new ArgumentException("Data connection id must be provided when copying a connection.");
        }

        var command = new OpenWindowCommand(WindowIds.DataConnection);
        command.Options.Height = WindowSizes.DataConnectionHeight;
        command.Options.Width = WindowSizes.DataConnectionWidth;

        if (dataConnectionId != null)
        {
            command.Metadata.Add("data-connection-id", dataConnectionId);
        }

        if (copy)
        {
            command.Metadata.Add("copy", "true");
        }

        if (isServer)
        {
            command.Metadata.Add("is-server", "true");
        }

        await ipcService.SendAsync(command);
    }

    public async Task OpenOutputWindowAsync()
    {
        var command = new OpenWindowCommand(WindowIds.Output);
        command.Options.Height = WindowSizes.OutputHeight;
        command.Options.Width = WindowSizes.OutputWidth;

        await ipcService.SendAsync(command);
    }

    public async Task OpenCodeWindowAsync()
    {
        var command = new OpenWindowCommand(WindowIds.Code);
        command.Options.Height = WindowSizes.CodeHeight;
        command.Options.Width = WindowSizes.CodeWidth;

        await ipcService.SendAsync(command);
    }
}
