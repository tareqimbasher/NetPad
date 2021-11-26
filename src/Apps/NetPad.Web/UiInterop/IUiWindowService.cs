using System.Threading.Tasks;
using NetPad.Scripts;

namespace NetPad.UiInterop
{
    public interface IUiWindowService
    {
        Task OpenMainWindowAsync();
        Task OpenScriptConfigWindowAsync(Script script);
    }
}
