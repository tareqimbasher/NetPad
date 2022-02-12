using NetPad.Commands;
using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.Web.UiInterop;

public class WebDialogService : IUiDialogService
{
    private readonly IIpcService _ipcService;

    public WebDialogService(IIpcService ipcService)
    {
        _ipcService = ipcService;
    }

    public async Task<YesNoCancel> AskUserIfTheyWantToSave(Script script)
    {
        return await _ipcService.SendAndReceiveAsync(new ConfirmSaveCommand(script));
    }

    public Task<string?> AskUserForSaveLocation(Script script)
    {
        return Task.FromResult($"/{script.Name}{Script.STANARD_EXTENSION}");
    }
}
