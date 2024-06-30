using System.Diagnostics;
using System.Reflection;
using NetPad.ExecutionModel.External.Interface;
using NetPad.Presentation;

/// <summary>
/// Meant to be injected into script code so it can initialize <see cref="ExternalProcessDumpSink"/>.
/// The class name must be "Program" and must be partial. This is so we augment the base "Program" class
/// .NET will implicitly wrap top-level statements within. Code in the constructor will be called by the runtime
/// before a script's code is executed.
///
/// This is embedded into the assembly to be read later as an Embedded Resource.
/// </summary>
public partial class Program
{
    public static readonly UserScript UserScript = new(
        new Guid("SCRIPT_ID"),
        "SCRIPT_NAME",
        "SCRIPT_LOCATION");

    static Program()
    {
        var args = Environment.GetCommandLineArgs();

        if (args.Contains("-help"))
        {
            ExternalProcessDumpSink.Instance.UseConsoleOutput(true);

            PrintHelp();

            Environment.Exit(0);
        }

        TerminateProcessOnParentExit(args);

        if (args.Contains("-html"))
        {
            ExternalProcessDumpSink.Instance.UseHtmlOutput();
        }
        else
        {
            bool useConsoleColors = !args.Contains("-no-color");

            if (args.Contains("-text"))
            {
                ExternalProcessDumpSink.Instance.UseTextOutput(useConsoleColors);
            }
            else
            {
                ExternalProcessDumpSink.Instance.UseConsoleOutput(useConsoleColors);
            }
        }

        DumpExtension.UseSink(ExternalProcessDumpSink.Instance);

        // Use "NetPad.Utilities" qualifier because NetPad.Utilities is a global using in NetPad.Runtime, but not in running script
        if (NetPad.Utilities.PlatformUtil.IsOSWindows())
        {
            NetPad.Utilities.WindowsNative.DisableWindowsErrorReporting();
        }
    }

    private static void PrintHelp()
    {
        var currentAssemblyPath = Assembly.GetExecutingAssembly().Location;
        if (Environment.CurrentDirectory.Length > 1)
        {
            currentAssemblyPath = "." + currentAssemblyPath.Replace(Environment.CurrentDirectory, string.Empty);
        }

        Console.WriteLine($"{UserScript.Name}");
        Console.WriteLine($@"
Usage:
    dotnet {currentAssemblyPath} [-console|-text|-html] [OPTIONS]

Output Format:
    -console        Optimized for console output (default)
    -text           Text output
    -html           HTML output

Options:
    -no-color       Do not color output. Does not apply to ""HTML"" format
    -parent <ID>    Instructs process to terminate itself when this process ID is terminated.
    -help           Display this help
");
    }

    private static void TerminateProcessOnParentExit(string[] args)
    {
        var parentIx = Array.IndexOf(args, "-parent");

        if (parentIx < 0)
        {
            // No parent
            return;
        }

        if (args.Length < parentIx + 1 || !int.TryParse(args[parentIx + 1], out var parentProcessId))
        {
            Console.Error.WriteLine("Invalid parent process ID");
            Environment.Exit(1);
            return;
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
    }
}
