using NetPad.Presentation;

// ReSharper disable RedundantNameQualifier
// ReSharper disable InvokeAsExtensionMethod

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
            System.Console.WriteLine("Args: " + string.Join(" ", args));
        }

        if (System.Linq.Enumerable.Contains(args, "-help"))
        {
            NetPad.ExecutionModel.External.Interface.ExternalProcessDumpSink.Instance.UseConsoleOutput(true);
            PrintHelp();
            System.Environment.Exit(0);
        }

        TerminateProcessOnParentExit(args);

        if (System.Linq.Enumerable.Contains(args, "-html") || System.Linq.Enumerable.Contains(args, "-html-msg"))
        {
            if (verbose) System.Console.WriteLine("Output: HTML");
            bool dumpRawHtml = System.Linq.Enumerable.Contains(args, "-html-msg") == false;
            NetPad.ExecutionModel.External.Interface.ExternalProcessDumpSink.Instance.UseHtmlOutput(dumpRawHtml);
        }
        else
        {
            bool useConsoleColors = !System.Linq.Enumerable.Contains(args, "-no-color");

            if (verbose) System.Console.WriteLine("Output: Console");
            NetPad.ExecutionModel.External.Interface.ExternalProcessDumpSink.Instance.UseConsoleOutput(
                useConsoleColors);
        }

        DumpExtension.UseSink(NetPad.ExecutionModel.External.Interface.ExternalProcessDumpSink.Instance);

        // Use "NetPad.Utilities" qualifier because NetPad.Utilities is a global using in NetPad.Runtime, but not in running script
        if (NetPad.Utilities.PlatformUtil.IsOSWindows())
        {
            NetPad.Utilities.WindowsNative.DisableWindowsErrorReporting();
        }

        if (verbose)
        {
            System.Console.WriteLine();
        }
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
    dotnet {currentAssemblyPath} [-console|-text|-html] [OPTIONS]

Output Format:
    -console        Optimized for console output (default)
    -html           HTML output
    -html-msg       HTML message output

Options:
    -no-color       Do not color output. Does not apply to ""HTML"" format
    -parent <ID>    Instructs process to terminate itself when this process ID is terminated.
    -help           Display this help
");
    }

    private static void TerminateProcessOnParentExit(string[] args)
    {
        var parentIx = System.Array.IndexOf(args, "-parent");

        if (parentIx < 0)
        {
            // No parent
            return;
        }

        if (args.Length < parentIx + 1 || !int.TryParse(args[parentIx + 1], out var parentProcessId))
        {
            System.Console.Error.WriteLine("Invalid parent process ID");
            System.Environment.Exit(1);
            return;
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
    }
}
