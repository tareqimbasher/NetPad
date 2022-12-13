using System;
using System.Threading.Tasks;
using NetPad.IO;

namespace NetPad.Runtimes
{
    public interface IScriptRuntime : IDisposable, IAsyncDisposable
    {
        Task<RunResult> RunScriptAsync(RunOptions runOptions);

        void AddOutput(IScriptOutput output);
        void RemoveOutput(IScriptOutput output);
    }
}
