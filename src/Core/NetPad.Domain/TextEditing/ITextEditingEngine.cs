using System.Threading.Tasks;
using NetPad.Scripts;

namespace NetPad.TextEditing
{
    public interface ITextEditingEngine
    {
        Task LoadAsync(Script script);
        Task Autocomplete();
    }
}