using NetPad.Application;
using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;
using NetPad.Configuration;
using NetPad.Scripts;

namespace NetPad.Apps.Shells.Tauri.UiInterop;

public class TauriDialogService(IIpcService ipcService, Settings settings) : IUiDialogService
{
    public async Task<YesNoCancel> AskUserIfTheyWantToSave(Script script)
    {
        return await ipcService.SendAndReceiveAsync(new ConfirmSaveCommand(script));
    }

    public async Task<string?> AskUserForSaveLocation(Script script)
    {
        var defaultPath = script.GetDefaultSavePath(settings.ScriptsDirectoryPath);

        var path = await ipcService.SendAndReceiveAsync(new RequestScriptSavePathCommand(
            script.Name,
            defaultPath));

        if (path == null || string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(path)))
        {
            return null;
        }

        path = path.TrimEnd(Path.PathSeparator);

        if (!path.EndsWith(Script.STANDARD_EXTENSION, StringComparison.InvariantCultureIgnoreCase))
        {
            path += Script.STANDARD_EXTENSION;
        }

        return path;
    }

    public async Task<bool> AskUserToOpenAsDuplicate(string newPath, string existingPath)
    {
        return await ipcService.SendAndReceiveAsync(new ConfirmOpenAsDuplicateCommand(newPath, existingPath));
    }

    public async Task AlertUserAboutMissingDependencies(AppDependencyCheckResult dependencyCheckResult)
    {
        await ipcService.SendAsync(new AlertUserAboutMissingAppDependencies(dependencyCheckResult));
    }
}
