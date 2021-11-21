using System.Linq;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;
using NetPad.Scripts;

namespace NetPad.Services
{
    public enum YesNoCancel
    {
        Yes, No, Cancel
    }

    public class UiScriptService : IUiScriptService
    {
        private readonly Settings _settings;

        public UiScriptService(Settings settings)
        {
            _settings = settings;
        }

        public async Task<YesNoCancel> AskUserIfTheyWantToSave(Script script)
        {
            var result = await Electron.Dialog.ShowMessageBoxAsync(Electron.WindowManager.BrowserWindows.First(),
                new MessageBoxOptions("Do you want to save?")
                {
                    Title = "Save?",
                    Buttons = new[] { "Yes", "No", "Cancel" },
                    Type = MessageBoxType.question
                });

            return (YesNoCancel)result.Response;
        }

        public async Task<string?> AskUserForSaveLocation(Script script)
        {
            var path = await Electron.Dialog.ShowSaveDialogAsync(Electron.WindowManager.BrowserWindows.First(), new SaveDialogOptions
            {
                Title = "Save Script",
                Message = "Where do you want to save this script?",
                NameFieldLabel = script.Name,
                Filters = new[] { new FileFilter { Name = "NetPad Script", Extensions = new[] { Script.STANARD_EXTENSION_WO_DOT } } },
                DefaultPath = _settings.ScriptsDirectoryPath
            });

            return path;
        }
    }
}
