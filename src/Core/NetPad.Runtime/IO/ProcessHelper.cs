using System.Diagnostics;

namespace NetPad.IO;

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

public static class ProcessHelper
{
    public static void KillIfRunning(this Process process)
    {
        if (process.IsProcessRunning())
        {
            process.Kill();
        }
    }

    public static ProcessStartResult Run(
        this ProcessStartInfo startInfo,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        bool isLongRunning = false,
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

        Task<int> processTask;

        if (isLongRunning)
        {
            processTask = Task.Factory.StartNew(() =>
            {
                try
                {
                    process.WaitForExit();

                    // Can throw if process was killed
                    return process.ExitCode;
                }
                catch
                {
                    return -1;
                }
            }, TaskCreationOptions.LongRunning);
        }
        else
        {
            processTask = Task.Run(async () =>
            {
                try
                {
                    await process.WaitForExitAsync(cancellationToken);

                    // Can throw if process was killed
                    return process.ExitCode;
                }
                catch
                {
                    return -1;
                }
            });
        }

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
}
