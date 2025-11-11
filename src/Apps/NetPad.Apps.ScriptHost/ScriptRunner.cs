using System.Reflection;
using System.Runtime.CompilerServices;
using NetPad.Assemblies;
using NetPad.ExecutionModel;
using NetPad.ExecutionModel.ClientServer;
using NetPad.ExecutionModel.ClientServer.Messages;
using NetPad.ExecutionModel.ScriptServices;
using NetPad.IO.IPC.Stdio;
using NetPad.Presentation;
using NetPad.Utilities;

namespace NetPad.Apps.ScriptHost;

public class ScriptRunner
{
    private readonly StdioIpcGateway _ipcGateway;
    private readonly HashSet<string> _scriptHostLoadedAssemblies = new();
    private TaskCompletionSource<string?>? _userInputRequest;
    private bool _firstRun = true;

    public ScriptRunner(StdioIpcGateway ipcGateway)
    {
        _ipcGateway = ipcGateway;
        DumpExtension.UseSink(ClientServerDumpSink.Instance);
    }

    public void Run(RunScriptMessage message)
    {
        Util.Stopwatch.Reset();

        Util.SetUserScript(new UserScript(
            message.ScriptId,
            message.ScriptName,
            message.ScriptFilePath,
            message.IsDirty
        ));

        ClientServerDumpSink.Instance.RedirectStdIO(
            str =>
            {
                _ipcGateway.Send(new ScriptOutputMessage(str));
                return Task.CompletedTask;
            },
            () =>
            {
                _userInputRequest = new TaskCompletionSource<string?>();
                _ipcGateway.Send(new RequestUserInputMessage());
                return _userInputRequest.Task.Result;
            }
        );

        if (_firstRun)
        {
            StartForwardingMemCacheItemInfoChanges();
            _firstRun = false;
        }

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

            Execute(message.ScriptAssemblyPath, message.ProbingPaths);

            _ipcGateway.Send(new ScriptRunCompleteMessage(
                RunResult.Success(Util.Stopwatch.ElapsedMilliseconds),
                Util.RestartHostOnEveryRun
            ));
        }
        catch (TargetInvocationException e) when (e.InnerException != null)
        {
            HandleRunException(e.InnerException);
        }
        catch (Exception e)
        {
            HandleRunException(e);
        }
        finally
        {
            GcUtil.CollectAndWait();

            Retry.Execute(
                3,
                TimeSpan.FromSeconds(1),
                () =>
                {
                    if (Directory.Exists(message.ScriptDirPath))
                    {
                        Directory.Delete(message.ScriptDirPath, true);
                    }
                });
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Execute(string scriptAssemblyPath, string[] probingPaths)
    {
        using var assemblyLoader = new UnloadableAssemblyLoadContext(scriptAssemblyPath);

        assemblyLoader.UseProbing(probingPaths);

        var assembly = assemblyLoader.LoadFromAssemblyPath(scriptAssemblyPath);

        Util.Stopwatch.Restart();

        assembly.EntryPoint!.Invoke(null, [Array.Empty<string>()]);

        Util.Stopwatch.Stop();
    }

    private void HandleRunException(Exception exception)
    {
        Util.Stopwatch.Stop();
        _ipcGateway.Send(new ScriptRunCompleteMessage(
            RunResult.ScriptCompletionFailure(Util.Stopwatch.ElapsedMilliseconds),
            Util.RestartHostOnEveryRun,
            exception.ToString()
        ));
    }

    public void ReceiveUserInput(ReceiveUserInputMessage message)
    {
        _userInputRequest?.TrySetResult(message.Input);
    }

    private void StartForwardingMemCacheItemInfoChanges()
    {
        var debounced = DelegateUtil.Debounce(
            () => _ipcGateway.Send(new MemCacheItemInfoChangedMessage(Util.Cache.GetItemInfos())),
            100);

        Util.Cache.MemCacheItemInfoChanged += (_, _) => debounced();
    }

    public static void DumpMemCacheItem(DumpMemCacheItemMessage message)
    {
        if (Util.Cache.TryGet(message.Key, out var value))
        {
            Util.Dump(value, new DumpOptions("Cache Key: " + message.Key)
            {
                Order = 0
            });
        }
    }

    public static void DeleteMemCacheItem(DeleteMemCacheItemMessage message)
    {
        Util.Cache.Remove(message.Key);
    }

    public static void ClearMemCache(ClearMemCacheMessage _)
    {
        Util.Cache.Clear();
    }
}
