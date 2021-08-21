using System.Threading.Tasks;
using NetPad.Queries;

namespace NetPad.Runtimes
{
    public interface IQueryRuntime
    {
        Task InitializeAsync(Query query);
        Task RunAsync(IQueryRuntimeInputReader inputReader, IQueryRuntimeOutputReader outputReader);
    }
}