using NetPad.Commands;
using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.Web.UiInterop;

public class WebWindowService : IUiWindowService
{
    private readonly IIpcService _ipcService;

    public WebWindowService(IIpcService ipcService)
    {
        _ipcService = ipcService;
    }

    public Task OpenMainWindowAsync()
    {
        throw new PlatformNotSupportedException();
    }

    public async Task OpenSettingsWindowAsync()
    {
        var command = new OpenWindowCommand("settings");
        command.Options.Height = 0.5;
        command.Options.Width = 0.5;

        await _ipcService.SendAsync(command);
    }

    public async Task OpenScriptConfigWindowAsync(Script script)
    {
        var command = new OpenWindowCommand("script-config");
        command.Options.Height = 2 / 3.0;
        command.Options.Width = 4 / 5.0;

        command.Metadata.Add("script-id", script.Id);

        await _ipcService.SendAsync(command);
    }
}
