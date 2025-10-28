// ReSharper disable RedundantNameQualifier
// ReSharper disable InvokeAsExtensionMethod
using NetPad.Presentation;
// ReSharper disable once RedundantUsingDirective
using Util = NetPad.ExecutionModel.ScriptServices.Util;


/// <summary>
/// Meant to be injected into script code so it can initialize <see cref="NetPad.ExecutionModel.External.Interface.ExternalProcessDumpSink"/>.
/// The class name must be "Program" and must be partial. This is so we augment the base "Program" class
/// .NET will implicitly wrap top-level statements within. Code in the constructor will be called by the runtime
/// before a script's code is executed.
///
/// This is embedded into the assembly to be read later as an Embedded Resource.
/// </summary>
public partial class Program
{
    public static readonly NetPad.ExecutionModel.External.Interface.UserScript UserScript = new(
        new System.Guid("SCRIPT_ID"),
        "SCRIPT_NAME",
        "SCRIPT_LOCATION");

    static Program()
    {
        var args = System.Environment.GetCommandLineArgs();
        bool verbose = System.Linq.Enumerable.Contains(args, "-verbose");

        if (verbose)
        {
            WriteColor("inf: ", ConsoleColor.Cyan);
            System.Console.Error.WriteLine(
                $"Script process started with args: {string.Join(" ", args)}");
        }

        if (System.Linq.Enumerable.Contains(args, "-help"))
        {
            NetPad.ExecutionModel.External.Interface.ExternalProcessDumpSink.Instance.UseConsoleOutput(true, false);
            PrintHelp();
            System.Environment.Exit(0);
        }

        var parentProcessId = TerminateProcessOnParentExit(args);

        if (System.Linq.Enumerable.Contains(args, "-html")
            || System.Linq.Enumerable.Contains(args, "-html-msg"))
        {
            if (verbose)
            {
                WriteColor("inf: ", ConsoleColor.Cyan);
                System.Console.Error.WriteLine("Output format: HTML");
            }

            bool dumpRaw = System.Linq.Enumerable.Contains(args, "-html");
            NetPad.ExecutionModel.External.Interface.ExternalProcessDumpSink.Instance.UseHtmlOutput(dumpRaw);
        }
        else
        {
            bool plainText = System.Linq.Enumerable.Contains(args, "-text");
            bool minimal = System.Linq.Enumerable.Contains(args, "-minimal");

            if (verbose)
            {
                WriteColor("inf: ", ConsoleColor.Cyan);
                System.Console.Error.WriteLine("Output format: " + (plainText ? "Text" : "Console"));
            }
            NetPad.ExecutionModel.External.Interface.ExternalProcessDumpSink.Instance.UseConsoleOutput(
                plainText,
                minimal);
        }

        DumpExtension.UseSink(NetPad.ExecutionModel.External.Interface.ExternalProcessDumpSink.Instance);

        // Use "NetPad.Utilities" qualifier because NetPad.Utilities is a global using in NetPad.Runtime, but not in running script
        if (NetPad.Utilities.PlatformUtil.IsOSWindows())
        {
            NetPad.Utilities.WindowsNative.DisableWindowsErrorReporting();
        }

        Util.Init(parentProcessId);
        Util.SetUserScript(new NetPad.ExecutionModel.ScriptServices.UserScript(
            Guid.Parse("USERSCRIPT_ID"),
            "USERSCRIPT_NAME",
            "USERSCRIPT_PATH",
            false
        ));

        if (verbose)
        {
            System.Console.WriteLine();
        }
    }

    private static void WriteColor(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        System.Console.Error.Write(text);
        Console.ResetColor();
    }

    private static void PrintHelp()
    {
        var currentAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        if (System.Environment.CurrentDirectory.Length > 1)
        {
            currentAssemblyPath = "." + currentAssemblyPath.Replace(System.Environment.CurrentDirectory, string.Empty);
        }

        System.Console.WriteLine($"{UserScript.Name}");
        System.Console.WriteLine($@"
Usage:
    dotnet {currentAssemblyPath} [OPTIONS]

Options:
    -console        Optimized for console output (default)
    -text           Output to plain text
    -html           Output in raw HTML
    -html-msg       Output in a message envelope with the body in HTML. For inter-process communication use.
    -minimal        If possible, use more minimal output formatting.

    -parent <ID>    Instructs process to terminate itself when this process ID is terminated.
    -help           Display this help
");
    }

    private static int? TerminateProcessOnParentExit(string[] args)
    {
        var parentIx = System.Array.IndexOf(args, "-parent");

        if (parentIx < 0)
        {
            // No parent
            return null;
        }

        if (args.Length < parentIx + 1 || !int.TryParse(args[parentIx + 1], out var parentProcessId))
        {
            System.Console.Error.WriteLine("Invalid parent process ID");
            System.Environment.Exit(1);
            return null;
        }

        System.Diagnostics.Process? parentProcess = null;

        try
        {
            parentProcess = System.Diagnostics.Process.GetProcessById(parentProcessId);
            parentProcess.EnableRaisingEvents = true;
        }
        catch
        {
            // ignore
        }

        if (parentProcess != null)
        {
            parentProcess.Exited += (_, _) => System.Environment.Exit(1);
        }
        else
        {
            System.Console.Error.WriteLine($"Parent process {parentProcessId} is not running");
            System.Environment.Exit(1);
        }

        return parentProcessId;
    }
}
