using ElectronSharp.API;
using ElectronSharp.API.Entities;
using NetPad.Application;
using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;
using NetPad.Configuration;
using NetPad.Scripts;

namespace NetPad.Apps.Shells.Electron.UiInterop;

public class ElectronDialogService(IIpcService ipcService, Settings settings) : IUiDialogService
{
    public async Task<YesNoCancel> AskUserIfTheyWantToSave(Script script)
    {
        var result = await ElectronSharp.API.Electron.Dialog.ShowMessageBoxAsync(ElectronUtil.MainWindow,
            new MessageBoxOptions($"'{script.Name}' has unsaved changes. Do you want to save?")
            {
                Title = "Save?",
                Buttons = ["Yes", "No", "Cancel"],
                Type = MessageBoxType.question
            });

        return (YesNoCancel)result.Response;
    }

    public async Task<string?> AskUserForSaveLocation(Script script)
    {
        var options = new SaveDialogOptions
        {
            Title = "Save Script",
            Filters = [new FileFilter { Name = "NetPad Script", Extensions = [Script.STANDARD_EXTENSION_WO_DOT] }],
            DefaultPath = Path.Combine(settings.ScriptsDirectoryPath, script.Name + Script.STANDARD_EXTENSION)
        };

        if (OperatingSystem.IsMacOS())
        {
            options.Message = "Where do you want to save this script?";
            options.NameFieldLabel = script.Name;
        }

        var path = await ElectronSharp.API.Electron.Dialog.ShowSaveDialogAsync(ElectronUtil.MainWindow, options);

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

    public async Task AlertUserAboutMissingDependencies(AppDependencyCheckResult dependencyCheckResult)
    {
        await ipcService.SendAsync(new AlertUserAboutMissingAppDependencies(dependencyCheckResult));
    }
}
