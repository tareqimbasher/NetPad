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
        var scriptName = script.Name;

        while (true)
        {
            var response = await ipcService.SendAndReceiveAsync(new RequestScriptSavePathCommand(scriptName));

            var name = Path.GetFileNameWithoutExtension(response);

            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var fullPath = Path.Combine(settings.ScriptsDirectoryPath, name + Script.STANDARD_EXTENSION);

            if (!File.Exists(fullPath))
            {
                return fullPath;
            }

            var retry = await ipcService.SendAndReceiveAsync(
                new ConfirmWithUserCommand($"A script named '{name}' already exists. Choose a different name."));

            if (retry != YesNoCancel.Yes)
            {
                return null;
            }

            scriptName = name;
        }
    }

    public async Task AlertUserAboutMissingDependencies(AppDependencyCheckResult dependencyCheckResult)
    {
        await ipcService.SendAsync(new AlertUserAboutMissingAppDependencies(dependencyCheckResult));
    }
}
