using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NetPad.IO;

public sealed class ProcessHandler : IDisposable
{
    private readonly ProcessStartInfo _processStartInfo;
    private Process? _process;
    private ProcessIO? _io;
    private Task<int>? _processStartTask;

    public ProcessHandler(string commandText, string? args)
    {
        if (string.IsNullOrWhiteSpace(commandText))
        {
            throw new ArgumentException("Cannot be null or empty", nameof(commandText));
        }

        _processStartInfo = new ProcessStartInfo(commandText)
            .WithRedirectIO()
            .WithNoUi()
            .CopyCurrentEnvironmentVariables();

        if (!string.IsNullOrWhiteSpace(args))
        {
            _processStartInfo.Arguments = args;
        }
    }

    public ProcessHandler(ProcessStartInfo processStartInfo)
    {
        _processStartInfo = processStartInfo ?? throw new ArgumentNullException(nameof(processStartInfo));
    }

    public Process Process
    {
        get
        {
            Init();
            return _process!;
        }
    }

    public ProcessStartInfo ProcessStartInfo => _processStartInfo;

    public ProcessIO IO
    {
        get
        {
            Init();
            return _io!;
        }
    }

    public ProcessStartResult StartProcess(CancellationToken cancellationToken = default)
    {
        Init();

        var process = _process!;

        if (process.IsProcessRunning())
            throw new InvalidOperationException(
                $"Process is already started and has not terminated yet. Process PID: {process.Id}.");

        process.Start();

        // We have to wait for the process or otherwise it exists shortly after its spawned
        _processStartTask = Task.Run(async () =>
        {
            await process.WaitForExitAsync(cancellationToken);

            try
            {
                return process.ExitCode;
            }
            catch
            {
                // Can throw if process is killed
                return -1;
            }
        });

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        bool started = process.IsProcessRunning();

        return new ProcessStartResult(started, _processStartTask);
    }

    public void StopProcess()
    {
        if (_process != null)
        {
            if (_process.IsProcessRunning())
            {
                _process.Kill();
            }

            _processStartTask = null;
            _process.Dispose();
            _process = null;
        }

        if (_io != null)
        {
            _io.Dispose();
            _io = null;
        }
    }

    public void Restart()
    {
        if (_process?.IsProcessRunning() == true)
        {
            StopProcess();
        }

        StartProcess();
    }

    private void Init()
    {
        if (_process == null)
        {
            _process = new Process
            {
                StartInfo = _processStartInfo,
                EnableRaisingEvents = true
            };

            _io = new ProcessIO(_process);
        }
    }

    public void Dispose()
    {
        StopProcess();
    }

    public static Task<ProcessRunResult> RunAsync(string fileName, string? arguments = null, CancellationToken cancellationToken = default)
    {
        var processStartInfo = arguments == null ? new ProcessStartInfo(fileName) : new ProcessStartInfo(fileName, arguments);
        processStartInfo.WithRedirectIO();
        processStartInfo.WithNoUi();

        return RunAsync(processStartInfo, cancellationToken);
    }

    public static async Task<ProcessRunResult> RunAsync(ProcessStartInfo processStartInfo, CancellationToken cancellationToken = default)
    {
        using var handler = new ProcessHandler(processStartInfo);

        var output = new List<string>();
        var errors = new List<string>();

        handler.IO.OnOutputReceivedHandlers.Add(text =>
        {
            output.Add(text);
            return Task.CompletedTask;
        });

        handler.IO.OnErrorReceivedHandlers.Add(text =>
        {
            errors.Add(text);
            return Task.CompletedTask;
        });

        var startResult = handler.StartProcess(cancellationToken);

        if (!startResult.Success)
        {
            return new ProcessRunResult(int.MinValue, output, new List<string>() { "Could not start process." });
        }

        await startResult.WaitForExitTask;

        return new ProcessRunResult(handler.Process.ExitCode, output, errors);
    }
}

public class ProcessStartResult
{
    public ProcessStartResult(bool success, Task<int> waitForExitTask)
    {
        Success = success;
        WaitForExitTask = waitForExitTask;
    }

    /// <summary>
    /// Indicates if the process was started successfully or not.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// A task that completes when the process terminates. It returns the process exit code.
    /// </summary>
    public Task<int> WaitForExitTask { get; }
}

public class ProcessRunResult
{
    public ProcessRunResult(int exitCode, List<string> output, List<string>? errors = null)
    {
        ExitCode = exitCode;
        Output = output;
        Errors = errors ?? new List<string>();
    }

    public int ExitCode { get; }
    public List<string> Output { get; }
    public List<string> Errors { get; }
    public bool Success => ExitCode == 0;
    public bool HasError => ExitCode != 0;

    public string? GetErrorMessage()
    {
        if (!HasError)
        {
            return null;
        }

        return !Errors.Any() ? Output.JoinToString(Environment.NewLine) : Errors.JoinToString(Environment.NewLine);
    }

    public void EnsureSuccessful()
    {
        if (HasError)
        {
            throw new Exception(GetErrorMessage());
        }
    }
}
