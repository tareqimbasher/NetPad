using System;
using System.Threading.Tasks;
using NetPad.IO;

namespace NetPad.Runtimes;

/// <summary>
/// An execution engine that runs <see cref="Scripts.Script"/>s.
/// </summary>
public interface IScriptRuntime : IDisposable
{
    Task<RunResult> RunScriptAsync(RunOptions runOptions);
    Task StopScriptAsync();

    /// <summary>
    /// Gets assemblies that expose supporting code to the running script specific to this runtime.
    /// </summary>
    /// <returns>Fully-qualified file paths of all support assemblies exposed by runtime.</returns>
    string[] GetUserAccessibleAssemblies();

    /// <summary>
    /// Adds an input reader that will be invoked whenever script makes a request for user input.
    /// </summary>
    void AddInput(IInputReader<string> inputReader);

    /// <summary>
    /// Removes a previously added input reader.
    /// </summary>
    void RemoveInput(IInputReader<string> inputReader);

    /// <summary>
    /// Adds an output writer that will be invoked whenever script, or this runtime itself, emits any output.
    /// </summary>
    void AddOutput(IOutputWriter<object> outputWriter);

    /// <summary>
    /// Removes a previously added output writer.
    /// </summary>
    void RemoveOutput(IOutputWriter<object> outputWriter);
}
