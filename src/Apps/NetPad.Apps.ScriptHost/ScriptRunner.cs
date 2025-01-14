using System.Reflection;
using System.Runtime.CompilerServices;
using NetPad.Assemblies;
using NetPad.ExecutionModel;
using NetPad.ExecutionModel.ClientServer;
using NetPad.ExecutionModel.ClientServer.Messages;
using NetPad.ExecutionModel.ClientServer.ScriptHost;
using NetPad.ExecutionModel.ClientServer.ScriptServices;
using NetPad.Presentation;
using NetPad.Utilities;

namespace NetPad.Apps.ScriptHost;

public class ScriptRunner
{
    private readonly ScriptHostIpcGateway _ipcGateway;
    private readonly HashSet<string> _scriptHostLoadedAssemblies = new();
    private TaskCompletionSource<string?>? _userInputRequest;

    public ScriptRunner(ScriptHostIpcGateway ipcGateway)
    {
        _ipcGateway = ipcGateway;
        DumpExtension.UseSink(ClientServerDumpSink.Instance);
    }

    public void Run(RunScriptMessage message)
    {
        Util.Stopwatch.Reset();

        Util.Script = new UserScript(
            message.ScriptId,
            message.ScriptName,
            message.ScriptFilePath,
            message.IsDirty
        );

        ClientServerDumpSink.Instance.RedirectStdIO(
            str =>
            {
                _ipcGateway.Send(0, new ScriptOutputMessage(str));
                return Task.CompletedTask;
            },
            () =>
            {
                _userInputRequest = new TaskCompletionSource<string?>();
                _ipcGateway.Send(0, new RequestUserInputMessage());
                return _userInputRequest.Task.Result;
            }
        );

        try
        {
            // Load script-host assemblies into default AssemblyLoadContext
            var scriptHostAssembliesToLoad = Directory.GetFiles(message.ScriptHostDepDirPath)
                .Where(f => Path.GetExtension(f).EqualsIgnoreCase(".dll") && !_scriptHostLoadedAssemblies.Contains(f));

            foreach (var file in scriptHostAssembliesToLoad)
            {
                Try.Run(() => Assembly.LoadFrom(file));
                _scriptHostLoadedAssemblies.Add(file);
            }

            Execute(message.ScriptAssemblyPath);

            _ipcGateway.Send(0, new ScriptRunCompleteMessage(
                RunResult.Success(Util.Stopwatch.ElapsedMilliseconds),
                Util.RestartHostOnEveryRun
            ));
        }
        catch (Exception e)
        {
            Util.Stopwatch.Stop();

            _ipcGateway.Send(0,
                new ScriptRunCompleteMessage(
                    RunResult.ScriptCompletionFailure(Util.Stopwatch.ElapsedMilliseconds),
                    Util.RestartHostOnEveryRun,
                    e.ToString()
                ));
        }
        finally
        {
            if (Directory.Exists(message.ScriptDirPath))
            {
                Retry.Execute(
                    3,
                    TimeSpan.FromSeconds(1),
                    () => Directory.Delete(message.ScriptDirPath, true));
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Execute(string scriptAssemblyPath)
    {
        using var assemblyLoader = new UnloadableAssemblyLoadContext(scriptAssemblyPath);

        var assembly = assemblyLoader.LoadFromAssemblyPath(scriptAssemblyPath);

        if (assembly.EntryPoint == null)
        {
            throw new InvalidOperationException("Executing entry point Name null");
        }

        Util.Stopwatch.Restart();

        assembly.EntryPoint!.Invoke(null, [Array.Empty<string>()]);

        Util.Stopwatch.Stop();
    }

    public void ReceiveUserInput(ReceiveUserInputMessage message)
    {
        _userInputRequest?.TrySetResult(message.Input);
    }
}
