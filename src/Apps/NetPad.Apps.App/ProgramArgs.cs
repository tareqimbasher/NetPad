using System.Linq;
using NetPad.Apps.Shells;
using NetPad.Apps.Shells.Electron;
using NetPad.Apps.Shells.Tauri;
using NetPad.Apps.Shells.Web;

namespace NetPad;

/// <summary>
/// Parsed program arguments.
/// </summary>
public class ProgramArgs
{
    public ProgramArgs(string[] args)
    {
        Raw = args;

        // When Electron.js app starts, it starts this process and passes the Electron IPC port.
        if (args.Any(a => a.ContainsIgnoreCase("/ELECTRONPORT")))
        {
            ShellType = ShellType.Electron;
        }
        // When the Tauri (rust) app starts, it starts this process and passes this option.
        else if (args.Any(a => a.EqualsIgnoreCase("--tauri")))
        {
            ShellType = ShellType.Tauri;
        }
        // If no shell can be determined, use web shell.
        else
        {
            ShellType = ShellType.Web;
        }

        RunMode = args.Contains("--swagger") ? RunMode.SwaggerGen : RunMode.Normal;

        var parentPidArg = Array.IndexOf(args, "--parent-pid");
        if (parentPidArg >= 0 && parentPidArg + 1 < args.Length)
        {
            if (int.TryParse(args[parentPidArg + 1], out var parentPid))
            {
                ParentPid = parentPid;
            }
            else
            {
                Console.WriteLine($"Invalid parent pid: {args[parentPidArg + 1]}");
                Environment.Exit((int)ProgramExitCode.InvalidParentProcessPid);
            }
        }
    }

    /// <summary>
    /// The raw args passed to the program.
    /// </summary>
    public string[] Raw { get; }

    /// <summary>
    /// The pid of the process that started this program.
    /// </summary>
    public int? ParentPid { get; }

    /// <summary>
    /// The mode to run the program in.
    /// </summary>
    public RunMode RunMode { get; }

    /// <summary>
    /// The shell the app is running in.
    /// </summary>
    public ShellType ShellType { get; }

    public IShell CreateShell()
    {
        if (ShellType == ShellType.Electron)
        {
            return new ElectronShell();
        }

        if (ShellType == ShellType.Tauri)
        {
            return new TauriShell();
        }

        return new WebBrowserShell();
    }
}
