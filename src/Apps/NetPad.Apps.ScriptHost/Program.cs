using System.Diagnostics;
using NetPad.Apps.ScriptHost;
using NetPad.Common;
using NetPad.ExecutionModel.ClientServer;
using NetPad.ExecutionModel.ClientServer.Messages;
using NetPad.ExecutionModel.ClientServer.ScriptHost;
using NetPad.ExecutionModel.ClientServer.ScriptServices;

var parentPid = TerminateProcessOnParentExit(args);
Util.Environment = new HostEnvironment(parentPid);

var defaultConsoleIn = Console.In;
var defaultConsoleOut = Console.Out;

var ipc = new ScriptHostIpcGateway(defaultConsoleOut);
var runner = new ScriptRunner(ipc);

ipc.Listen(
    defaultConsoleIn,
    OnMessageFromServer,
    _ => { });

ipc.Send(0, new ScriptHostReadyMessage());

// Wait till app exits
await new TaskCompletionSource().Task;
return;


void OnMessageFromServer(ScriptHostIpcMessage message)
{
    if (message.Type == typeof(RunScriptMessage).FullName)
    {
        runner.Run(JsonSerializer.Deserialize<RunScriptMessage>(message.Data) ??
                   throw new Exception("Could not deserialize message to RunScriptMessage"));
    }

    if (message.Type == typeof(ReceiveUserInputMessage).FullName)
    {
        runner.ReceiveUserInput(JsonSerializer.Deserialize<ReceiveUserInputMessage>(message.Data) ??
                                throw new Exception("Could not deserialize message to ReceiveUserInputMessage"));
    }
}

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
