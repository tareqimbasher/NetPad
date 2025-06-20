using System.Diagnostics;

namespace NetPad.Utilities;

public static class ProcessUtil
{
    /// <summary>
    /// Returns <c>true</c> if the process is currently running or was started at some point.
    /// </summary>
    public static bool WasProcessStarted(this Process process)
    {
        try
        {
            _ = process.HasExited;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static bool IsProcessRunning(this Process process)
    {
        return process.WasProcessStarted() && !process.HasExited;
    }

    public static void KillIfRunning(this Process process)
    {
        if (process.IsProcessRunning())
        {
            process.Kill();
        }
    }

    /// <summary>
    /// Opens a file, directory or url using the default application configured by the user's desktop environment
    /// or window manager.
    /// </summary>
    /// <param name="path">The path to a file, directory or url to open.</param>
    public static void OpenWithDefaultApp(string path)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    /// <summary>
    /// Copies current environment variables to the specified <see cref="ProcessStartInfo"/>.
    /// </summary>
    /// <param name="processStartInfo"></param>
    /// <param name="overwrite"></param>
    /// <returns></returns>
    public static ProcessStartInfo CopyCurrentEnvironmentVariables(
        this ProcessStartInfo processStartInfo,
        bool overwrite = true)
    {
        var envVars = Environment.GetEnvironmentVariables();

        foreach (string key in envVars.Keys)
        {
            if (!overwrite && processStartInfo.EnvironmentVariables.ContainsKey(key)) continue;

            processStartInfo.EnvironmentVariables[key] = envVars[key]?.ToString();
        }

        return processStartInfo;
    }

    /// <summary>
    /// Attempts to make a file executable. Does nothing on Microsoft Windows.
    /// </summary>
    /// <param name="filePath"></param>
    public static void MakeExecutable(string filePath)
    {
        if (PlatformUtil.IsOSWindows())
        {
            return;
        }

        using var p = Process.Start("chmod", $"+x {filePath}");
        p.WaitForExit();
    }

    public static ProcessStartInfo WithWorkingDirectory(this ProcessStartInfo processStartInfo, string workingDirectory)
    {
        processStartInfo.WorkingDirectory = workingDirectory;
        return processStartInfo;
    }

    public static ProcessStartInfo WithRedirectIO(this ProcessStartInfo processStartInfo)
    {
        processStartInfo.RedirectStandardInput = true;
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;

        return processStartInfo;
    }

    public static ProcessStartInfo WithNoUi(this ProcessStartInfo processStartInfo)
    {
        processStartInfo.CreateNoWindow = true;
        processStartInfo.UseShellExecute = false;
        processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

        return processStartInfo;
    }

    /// <summary>
    /// Starts a process using the specified <see cref="ProcessStartInfo"/> and returns a
    /// <see cref="ProcessStartResult"/> for observing its run state. Optionally attaches callbacks to capture
    /// redirected output and error streams.
    /// </summary>
    /// <param name="startInfo">
    /// The <see cref="ProcessStartInfo"/> that describes how to start the process. Must not be <c>null</c>.
    /// </param>
    /// <param name="onOutput">
    /// An optional <see cref="Action{String}"/> invoked for each line of redirected standard output.
    /// Only subscribed if <see cref="ProcessStartInfo.RedirectStandardOutput"/> is <c>true</c> and this callback is non-<c>null</c>.
    /// </param>
    /// <param name="onError">
    /// An optional <see cref="Action{String}"/> invoked for each line of redirected standard error.
    /// Only subscribed if <see cref="ProcessStartInfo.RedirectStandardError"/> is <c>true</c> and this callback is non-<c>null</c>.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that, when signaled, cancels the asynchronous wait for the process to exit.
    /// </param>
    /// <remarks>
    /// Before starting the process, this method wires up <c>OutputDataReceived</c> and/or
    /// <c>ErrorDataReceived</c> event handlers if the corresponding redirects and callbacks are provided.
    /// After <see cref="Process.Start()"/>, it begins reading redirected streams via
    /// <c>BeginOutputReadLine()</c> and <c>BeginErrorReadLine()</c>, then returns immediately along with
    /// a task that completes once <c>WaitForProcessToExitAsync</c> observes process termination or
    /// cancellation via <paramref name="cancellationToken"/>.
    /// </remarks>
    public static ProcessStartResult Run(
        this ProcessStartInfo startInfo,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default)
    {
        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        if (process == null)
        {
            throw new Exception($"Could not start process: {startInfo.FileName}");
        }

        if (startInfo.RedirectStandardOutput && onOutput != null)
        {
            process.OutputDataReceived += (_, ev) =>
            {
                var output = ev.Data;

                if (output == null)
                {
                    return;
                }

                onOutput(output);
            };
        }

        if (startInfo.RedirectStandardError && onError != null)
        {
            process.ErrorDataReceived += (_, ev) =>
            {
                var output = ev.Data;

                if (output == null)
                {
                    return;
                }

                onError(output);
            };
        }

        process.Start();

        // Get back a task that completes when process exits.
        var processTask = WaitForProcessToExitAsync(process, cancellationToken);

        if (startInfo.RedirectStandardOutput)
        {
            process.BeginOutputReadLine();
        }

        if (startInfo.RedirectStandardError)
        {
            process.BeginErrorReadLine();
        }

        return new ProcessStartResult(process.IsProcessRunning(), process, processTask);
    }

    private static async Task<int> WaitForProcessToExitAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode;
        }
        catch
        {
            // Process was killed or errored or caller requested cancellation
            return -1;
        }
    }
}

public class ProcessStartResult(bool started, Process process, Task<int> waitForExitTask)
{
    /// <summary>
    /// Indicates if the process was started successfully or not.
    /// </summary>
    public bool Started { get; } = started;

    public Process Process { get; } = process;

    /// <summary>
    /// A task that completes when the process terminates. It returns the process exit code.
    /// </summary>
    public Task<int> WaitForExitTask { get; } = waitForExitTask;
}
