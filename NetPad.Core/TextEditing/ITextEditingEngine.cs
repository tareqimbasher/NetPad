using System.Threading.Tasks;
using NetPad.Queries;

namespace NetPad.TextEditing
{
    public interface ITextEditingEngine
    {
        Task LoadAsync(Query query);
        Task Autocomplete();
    }
}