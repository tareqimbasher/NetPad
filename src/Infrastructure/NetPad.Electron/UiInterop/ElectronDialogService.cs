using ElectronNET.API;
using ElectronNET.API.Entities;
using NetPad.Application;
using NetPad.Configuration;
using NetPad.CQs;
using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.Electron.UiInterop;

public class ElectronDialogService : IUiDialogService
{
    private readonly IIpcService _ipcService;
    private readonly Settings _settings;

    public ElectronDialogService(IIpcService ipcService, Settings settings)
    {
        _ipcService = ipcService;
        _settings = settings;
    }

    public async Task<YesNoCancel> AskUserIfTheyWantToSave(Script script)
    {
        var result = await ElectronNET.API.Electron.Dialog.ShowMessageBoxAsync(ElectronUtil.MainWindow,
            new MessageBoxOptions($"You have unsaved changes. Do you want to save '{script.Name}'?")
            {
                Title = "Save?",
                Buttons = new[] { "Yes", "No", "Cancel" },
                Type = MessageBoxType.question
            });

        return (YesNoCancel)result.Response;
    }

    public async Task<string?> AskUserForSaveLocation(Script script)
    {
        var path = await ElectronNET.API.Electron.Dialog.ShowSaveDialogAsync(ElectronUtil.MainWindow, new SaveDialogOptions
        {
            Title = "Save Script",
            Message = "Where do you want to save this script?",
            NameFieldLabel = script.Name,
            Filters = new[] { new FileFilter { Name = "NetPad Script", Extensions = new[] { Script.STANDARD_EXTENSION_WO_DOT } } },
            DefaultPath = Path.Combine(_settings.ScriptsDirectoryPath, script.Name + Script.STANDARD_EXTENSION)
        });

        if (path == null || string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(path)))
            return null;

        path = path.TrimEnd(Path.PathSeparator);

        if (!path.EndsWith(Script.STANDARD_EXTENSION, StringComparison.InvariantCultureIgnoreCase))
            path += Script.STANDARD_EXTENSION;

        return path;
    }

    public async Task AlertUserAboutMissingDependencies(AppDependencyCheckResult dependencyCheckResult)
    {
        await _ipcService.SendAsync(new AlertUserAboutMissingAppDependencies(dependencyCheckResult));
    }
}
