using NetPad.Application;
using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;
using NetPad.Configuration;
using NetPad.Scripts;

namespace NetPad.Apps.Shells.Web.UiInterop;

public class WebDialogService(IIpcService ipcService, Settings settings) : IUiDialogService
{
    public async Task<YesNoCancel> AskUserIfTheyWantToSave(Script script)
    {
        return await ipcService.SendAndReceiveAsync(new ConfirmSaveCommand(script));
    }

    public async Task<string?> AskUserForSaveLocation(Script script)
    {
        var path = await ipcService.SendAndReceiveAsync(new RequestScriptSavePathCommand(script.Name));

        var name = Path.GetFileNameWithoutExtension(path);

        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return Path.Combine(settings.ScriptsDirectoryPath, name + Script.STANDARD_EXTENSION);
    }

    public async Task AlertUserAboutMissingDependencies(AppDependencyCheckResult dependencyCheckResult)
    {
        await ipcService.SendAsync(new AlertUserAboutMissingAppDependencies(dependencyCheckResult));
    }
}
