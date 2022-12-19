using System;
using System.Threading.Tasks;
using NetPad.IO;

namespace NetPad.Runtimes
{
    /// <summary>
    /// Handles all operations related to running a <see cref="Scripts.Script"/>.
    /// </summary>
    public interface IScriptRuntime : IDisposable, IAsyncDisposable
    {
        Task<RunResult> RunScriptAsync(RunOptions runOptions);
        Task StopScriptAsync();
    }

    /// <summary>
    /// Handles all operations related to running a <see cref="Scripts.Script"/>. This runtime
    /// also specifies what type of <see cref="IScriptOutputAdapter"/> the runtime uses.
    /// </summary>
    public interface IScriptRuntime<in TScriptOutputAdapter> : IScriptRuntime where TScriptOutputAdapter : IScriptOutputAdapter
    {
        void AddOutput(TScriptOutputAdapter outputAdapter);
        void RemoveOutput(TScriptOutputAdapter outputAdapter);
    }
}
