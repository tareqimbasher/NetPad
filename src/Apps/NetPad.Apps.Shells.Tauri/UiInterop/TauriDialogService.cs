using NetPad.Application;
using NetPad.Apps.CQs;
using NetPad.Apps.Scripts;
using NetPad.Apps.UiInterop;
using NetPad.Configuration;
using NetPad.Scripts;

namespace NetPad.Apps.Shells.Tauri.UiInterop;

public class TauriDialogService(IIpcService ipcService, Settings settings, IScriptSerializerFactory serializerFactory) : IUiDialogService
{
    public async Task<YesNoCancel> AskUserIfTheyWantToSave(Script script)
    {
        return await ipcService.SendAndReceiveAsync(new ConfirmSaveCommand(script));
    }

    public async Task<string?> AskUserForSaveLocation(Script script)
    {
        var defaultExt = serializerFactory.GetDefault().FileExtension;

        var path = await ipcService.SendAndReceiveAsync(new RequestScriptSavePathCommand(
            script.Name,
            Path.Combine(settings.ScriptsDirectoryPath, script.Name + defaultExt)));

        if (path == null || string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(path)))
        {
            return null;
        }

        path = path.TrimEnd(Path.PathSeparator);

        if (!serializerFactory.IsKnownScriptPath(path))
        {
            path += defaultExt;
        }

        return path;
    }

    public async Task AlertUserAboutMissingDependencies(AppDependencyCheckResult dependencyCheckResult)
    {
        await ipcService.SendAsync(new AlertUserAboutMissingAppDependencies(dependencyCheckResult));
    }
}
