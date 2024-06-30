using NetPad.Application;
using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;
using NetPad.Scripts;

namespace NetPad.Apps.Shells.Web.UiInterop;

public class WebDialogService(IIpcService ipcService) : IUiDialogService
{
    public async Task<YesNoCancel> AskUserIfTheyWantToSave(Script script)
    {
        return await ipcService.SendAndReceiveAsync(new ConfirmSaveCommand(script));
    }

    public async Task<string?> AskUserForSaveLocation(Script script)
    {
        var newName = await ipcService.SendAndReceiveAsync(new RequestNewScriptNameCommand(script.Name));

        if (newName == null)
            return null;

        return $"/{newName}{Script.STANDARD_EXTENSION}";
    }

    public async Task AlertUserAboutMissingDependencies(AppDependencyCheckResult dependencyCheckResult)
    {
        await ipcService.SendAsync(new AlertUserAboutMissingAppDependencies(dependencyCheckResult));
    }
}
