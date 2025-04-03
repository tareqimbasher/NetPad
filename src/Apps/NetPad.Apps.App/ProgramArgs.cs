using System.Linq;
using NetPad.Apps.Shells;
using NetPad.Apps.Shells.Electron;
using NetPad.Apps.Shells.Tauri;
using NetPad.Apps.Shells.Web;

namespace NetPad;

public class ProgramArgs
{
    public ProgramArgs(string[] args)
    {
        Raw = args;

        ShellType = Raw.Any(a => a.ContainsIgnoreCase("/ELECTRONPORT"))
            ? ShellType.Electron
            : Raw.Any(a => a.EqualsIgnoreCase("--tauri"))
                ? ShellType.Tauri
                : ShellType.Web;

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
