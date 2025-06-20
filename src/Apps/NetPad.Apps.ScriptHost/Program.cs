using System.Diagnostics;
using NetPad.Apps.ScriptHost;
using NetPad.ExecutionModel.ClientServer.Messages;
using NetPad.ExecutionModel.ClientServer.ScriptHost;
using NetPad.ExecutionModel.ClientServer.ScriptServices;

var parentPid = TerminateProcessOnParentExit(args);

// Init environment information that can be used in user scripts.
Util.Environment = new HostEnvironment(parentPid);

// IO streams are used for IPC (inter-process communication) between this process and the parent (NetPad) process.
var defaultConsoleIn = Console.In;
var defaultConsoleOut = Console.Out;

// Create the interface which will be used for two-way communication with parent.
var ipc = new ScriptHostIpcGateway(defaultConsoleOut);

// Listen for messages from parent.
var runner = new ScriptRunner(ipc);
ipc.On<RunScriptMessage>(msg => runner.Run(msg));
ipc.On<ReceiveUserInputMessage>(msg => runner.ReceiveUserInput(msg));
ipc.On<DumpMemCacheItemMessage>(msg => runner.DumpMemCacheItem(msg));
ipc.On<DeleteMemCacheItemMessage>(msg => runner.DeleteMemCacheItem(msg));
ipc.On<ClearMemCacheMessage>(msg => runner.ClearMemCache(msg));
ipc.Listen(defaultConsoleIn, _ => { });

// Notify parent that this process (script-host) is ready.
ipc.Send(0, new ScriptHostReadyMessage());

// Wait indefinitely. This process will terminate when the parent process terminates it, or when the parent process
// itself is terminated.
await new TaskCompletionSource().Task;
return;

int TerminateProcessOnParentExit(string[] args)
{
    var parentIx = Array.IndexOf(args, "--parent");
    if (parentIx < 0)
    {
        Console.Error.WriteLine("No parent process ID specified");
        return 0;
    }

    if (args.Length < parentIx + 1 || !int.TryParse(args[parentIx + 1], out var parentProcessId))
    {
        Console.Error.WriteLine("Invalid parent process ID");
        Environment.Exit(1);
        return 0;
    }

    Process? parentProcess = null;

    try
    {
        parentProcess = Process.GetProcessById(parentProcessId);
        parentProcess.EnableRaisingEvents = true;
    }
    catch
    {
        // ignore
    }

    if (parentProcess != null)
    {
        parentProcess.Exited += (_, _) => Environment.Exit(1);
    }
    else
    {
        Console.Error.WriteLine($"Parent process {parentProcessId} is not running");
        Environment.Exit(1);
    }

    return parentProcessId;
}
