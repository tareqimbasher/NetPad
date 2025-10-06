using NetPad.IO;

namespace NetPad.ExecutionModel;

/// <summary>
/// Runs a script.
/// </summary>
public interface IScriptRunner : IDisposable
{
    /// <summary>
    /// Start running script.
    /// </summary>
    /// <returns>A task that completes when script run completes.</returns>
    Task<RunResult> RunScriptAsync(RunOptions runOptions);

    /// <summary>
    /// Stop the running script.
    /// </summary>
    Task StopScriptAsync();

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

    void DumpMemCacheItem(string key);
    void DeleteMemCacheItem(string key);
    void ClearMemCacheItems();
}
