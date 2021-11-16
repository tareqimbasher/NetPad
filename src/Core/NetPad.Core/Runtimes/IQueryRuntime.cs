using System;
using System.Threading.Tasks;
using NetPad.Queries;

namespace NetPad.Runtimes
{
    public interface IQueryRuntime : IDisposable
    {
        Task InitializeAsync(Query query);
        Task RunAsync(IQueryRuntimeInputWriter inputReader, IQueryRuntimeOutputReader outputReader);
    }
}
