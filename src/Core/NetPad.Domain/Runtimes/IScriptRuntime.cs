using System;
using System.Threading.Tasks;
using NetPad.Scripts;

namespace NetPad.Runtimes
{
    public interface IScriptRuntime : IDisposable
    {
        Task InitializeAsync(Script script);
        Task<RunResult> RunAsync(IScriptRuntimeInputReader inputReader, IScriptRuntimeOutputWriter outputWriter);
    }
}
