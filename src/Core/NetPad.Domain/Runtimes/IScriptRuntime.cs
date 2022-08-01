using System;
using System.Threading.Tasks;
using NetPad.IO;

namespace NetPad.Runtimes
{
    public interface IScriptRuntime : IDisposable, IAsyncDisposable
    {
        Task<RunResult> RunScriptAsync(RunOptions runOptions);

        void AddOutputListener(IOutputWriter outputWriter);
        void RemoveOutputListener(IOutputWriter outputWriter);
    }
}
