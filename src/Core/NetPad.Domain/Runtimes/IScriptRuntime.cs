using System;
using System.Threading.Tasks;
using NetPad.IO;

namespace NetPad.Runtimes;

/// <summary>
/// Handles all operations related to running a <see cref="Scripts.Script"/>.
/// </summary>
public interface IScriptRuntime : IDisposable
{
    Task<RunResult> RunScriptAsync(RunOptions runOptions);
    Task StopScriptAsync();

    void AddInput(IInputReader<string> outputAdapter);
    void RemoveInput(IInputReader<string> outputAdapter);
    void AddOutput(IOutputWriter<object> outputAdapter);
    void RemoveOutput(IOutputWriter<object> outputAdapter);
}
