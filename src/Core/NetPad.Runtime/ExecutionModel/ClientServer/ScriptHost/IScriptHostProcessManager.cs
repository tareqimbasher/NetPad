using NetPad.IO;

namespace NetPad.ExecutionModel.ClientServer.ScriptHost;

/// <summary>
/// Manages a single instance of the script-host process and provides a high-level
/// interface to send and receive messages to and from it.
/// </summary>
public interface IScriptHostProcessManager
{
    bool IsScriptHostRunning();

    void RunScript(
        Guid runId,
        DirectoryPath scriptHostDepsDir,
        DirectoryPath scriptDir,
        FilePath scriptAssemblyPath,
        string[] probingPaths);

    void Send<T>(T message) where T : class;

    void StopScriptHost();
}
