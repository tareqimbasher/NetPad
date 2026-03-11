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

        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(1000);
                Console.WriteLine($"ALC alive? {alcRefs.Count} > {alcRefs.All(x => x.IsAlive)}");
            }
        });
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

        if (_firstRun)
        {
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

            StartForwardingMemCacheItemInfoChanges();
            _firstRun = false;
        }
        else
        {
            ClientServerDumpSink.Instance.ResetCounters();
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

            for (int i = 0; alcRefs.Any(x => x.IsAlive) && i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

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

    List<WeakReference> alcRefs = new();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Execute(string scriptAssemblyPath, string[] probingPaths)
    {
        using var assemblyLoader = new UnloadableAssemblyLoadContext(scriptAssemblyPath);
        alcRefs.Add(new WeakReference(assemblyLoader, trackResurrection: false));

        assemblyLoader.UseProbing(probingPaths);

        var assembly = assemblyLoader.LoadFromAssemblyPath(scriptAssemblyPath);

        Util.Stopwatch.Restart();

        assembly.EntryPoint!.Invoke(null, [Array.Empty<string>()]);

        var programType = assembly.GetType("Program", throwOnError: true)!;
        programType.GetMethod("Cleanup", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(
            null, Array.Empty<object>());
        // var prop = programType.GetProperty(
        //     "DataContext",
        //     BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        //
        // if (prop is null)
        //     throw new MissingMemberException(programType.FullName, "DataContext");
        //
        // // Get current value
        // var ctx = prop.GetValue(null);
        // if (ctx is null)
        //     return;
        //
        // try
        // {
        //     // Prefer DisposeAsync if present
        //     if (ctx is IAsyncDisposable asyncDisposable)
        //     {
        //         // If you can make Execute async, await this instead.
        //         asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
        //     }
        //     else if (ctx is IDisposable disposable)
        //     {
        //         disposable.Dispose();
        //     }
        //     else
        //     {
        //         // Last-resort reflection (in case type identity prevents interface cast)
        //         var dispose = ctx.GetType().GetMethod("Dispose", Type.EmptyTypes);
        //         dispose?.Invoke(ctx, null);
        //     }
        // }
        // finally
        // {
        //     // Important: clear static to release reference and help ALC unload
        //     if (prop.CanWrite)
        //     {
        //         prop.SetValue(null, null);
        //         Console.WriteLine("Set to NULL");
        //     }
        //     else
        //     {
        //         // If it's get-only, see note below: you should add a Clear method in plugin.
        //     }
        // }

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
