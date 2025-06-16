using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace NetPad;

/// <summary>
/// Used to exit this program when the parent process that started it exits.
/// </summary>
public static class ParentProcessTracker
{
    private static bool _initialized;
    private static IHost? _thisHost;

    public static void ExitWhenParentProcessExists(int parentPid)
    {
        if (_initialized)
        {
            throw new InvalidOperationException("Already initialized");
        }

        Process? parentProcess = null;

        try
        {
            parentProcess = Process.GetProcessById(parentPid);
            parentProcess.EnableRaisingEvents = true;
        }
        catch
        {
            // ignore
        }

        if (parentProcess != null)
        {
            parentProcess.Exited += async (_, _) =>
            {
                try
                {
                    if (_thisHost != null)
                    {
                        await _thisHost.StopAsync(TimeSpan.FromSeconds(10));
                    }
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    Environment.Exit((int)ProgramExitCode.Success);
                }
            };

            _initialized = true;
        }
        else
        {
            Console.WriteLine($"Parent process with ID '{parentPid}' is not running");
            Environment.Exit((int)ProgramExitCode.ParentProcessIsNotRunning);
        }
    }

    public static void SetThisHost(IHost host)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Not initialized");
        }

        _thisHost = host;
    }
}
