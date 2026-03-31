using Microsoft.Extensions.Logging;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.IO.IPC.Stdio;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.ClientServer.ScriptHost;

public interface IScriptHostProcessManagerFactory
{
    IScriptHostProcessManager Create(
        Script script,
        WorkingDirectory workingDirectory,
        Action<StdioIpcGateway> addMessageHandlers,
        Action<string> nonMessageOutputHandler,
        Action<string> errorOutputHandler);
}

public class ScriptHostProcessManagerFactory(
    IEventBus eventBus,
    IDotNetInfo dotNetInfo,
    ILoggerFactory loggerFactory) : IScriptHostProcessManagerFactory
{
    public IScriptHostProcessManager Create(
        Script script,
        WorkingDirectory workingDirectory,
        Action<StdioIpcGateway> addMessageHandlers,
        Action<string> nonMessageOutputHandler,
        Action<string> errorOutputHandler)
    {
        return new ScriptHostProcessManager(
            script,
            workingDirectory,
            addMessageHandlers,
            nonMessageOutputHandler,
            errorOutputHandler,
            eventBus,
            dotNetInfo,
            loggerFactory);
    }
}
