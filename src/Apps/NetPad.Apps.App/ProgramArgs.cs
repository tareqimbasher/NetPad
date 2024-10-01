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

    public IShell CreateShell()
    {
        if (Raw.Any(a => a.ContainsIgnoreCase("/ELECTRONPORT")))
        {
            return new ElectronShell();
        }

        if (Raw.Any(a => a.EqualsIgnoreCase("--tauri")))
        {
            return new TauriShell();
        }

        return new WebBrowserShell();
    }
}
