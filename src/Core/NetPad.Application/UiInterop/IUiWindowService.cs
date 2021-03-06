using System.Threading.Tasks;
using NetPad.Scripts;

namespace NetPad.UiInterop
{
    public interface IUiWindowService
    {
        Task OpenMainWindowAsync();
        Task OpenSettingsWindowAsync(string? tab = null);
        Task OpenScriptConfigWindowAsync(Script script, string? tab = null);
    }
}
