using NetPad.IO;

namespace NetPad.ExecutionModel;

/// <summary>
/// Runs a script.
/// </summary>
public interface IScriptRunner : IDisposable
{
    Task<RunResult> RunScriptAsync(RunOptions runOptions);
    Task StopScriptAsync();

    /// <summary>
    /// Gets assemblies that users can reference in scripts.
    ///
    /// This is different than the assemblies or nuget packages users have added to their scripts. These
    /// assemblies are provided by NetPad. If we want an assembly that is packaged with NetPad to be
    /// accessible to user code, we add it here.
    /// </summary>
    /// <returns>Fully-qualified file paths of all user-visible assemblies.</returns>
    string[] GetUserVisibleAssemblies();

    /// <summary>
    /// Adds an input reader that will be invoked whenever script makes a request for user input.
    /// </summary>
    void AddInput(IInputReader<string> inputReader);

    /// <summary>
    /// Removes a previously added input reader.
    /// </summary>
    void RemoveInput(IInputReader<string> inputReader);

    /// <summary>
    /// Adds an output writer that will be invoked whenever output is emitted.
    /// </summary>
    void AddOutput(IOutputWriter<object> outputWriter);

    /// <summary>
    /// Removes a previously added output writer.
    /// </summary>
    void RemoveOutput(IOutputWriter<object> outputWriter);
}
