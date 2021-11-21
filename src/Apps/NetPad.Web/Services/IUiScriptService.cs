using System.Threading.Tasks;
using NetPad.Scripts;

namespace NetPad.Services
{
    public interface IUiScriptService
    {
        Task<YesNoCancel> AskUserIfTheyWantToSave(Script script);
        Task<string?> AskUserForSaveLocation(Script script);
    }
}
