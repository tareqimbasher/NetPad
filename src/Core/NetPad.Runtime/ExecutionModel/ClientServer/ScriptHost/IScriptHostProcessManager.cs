using NetPad.Exceptions;
using NetPad.IO;

namespace NetPad.ExecutionModel.ClientServer.ScriptHost;

/// <summary>
/// Manages a single instance of the script-host process and provides a high-level
/// interface to send and receive messages to and from it.
/// </summary>
public interface IScriptHostProcessManager
{
    /// <summary>Checks whether the script-host is currently running.</summary>
    bool IsScriptHostRunning();

    /// <summary>
    /// Checks that script-host can be started for the script, without starting it.
    /// </summary>
    /// <exception cref="CannotRunScriptException">Something script-host needs to start is not available.</exception>
    void EnsureCanStart();

    void RunScript(
        Guid runId,
        DirectoryPath scriptHostDepsDir,
        DirectoryPath scriptDir,
        FilePath scriptAssemblyPath,
        string[] probingPaths);

    /// <summary>Sends a message to the script-host.</summary>
    void Send<T>(T message) where T : class;

    void StopScriptHost();
}
