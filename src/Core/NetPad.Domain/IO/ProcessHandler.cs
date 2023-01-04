using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NetPad.Utilities;

namespace NetPad.IO;

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

public sealed class ProcessHandler : IDisposable
{
    private readonly string? _commandText;
    private readonly string? _args;
    private Process? _process;
    private ProcessIO? _io;
    private ProcessStartInfo? _processStartInfo;
    private Task<int>? _processStartTask;
    private bool _isDisposed;

    public ProcessHandler(string commandText) : this(commandText, null)
    {
        _commandText = commandText ?? throw new ArgumentNullException(nameof(commandText));
    }

    public ProcessHandler(string commandText, string? args)
    {
        _commandText = commandText ?? throw new ArgumentNullException(nameof(commandText));
        _args = args;
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

    public ProcessIO IO
    {
        get
        {
            Init();
            return _io!;
        }
    }

    public ProcessStartResult StartProcess()
    {
        EnsureNotDisposed();

        Init();

        var process = _process!;

        if (process.IsProcessRunning())
            throw new InvalidOperationException(
                $"Process is already started and has not terminated yet. Process PID: {process.Id}.");

        process.Start();

        // We have to wait for the process or otherwise it exists shortly after its spawned
        _processStartTask = Task.Run(() =>
        {
            process.WaitForExit();

            try
            {
                return process.ExitCode;
            }
            catch (Exception ex)
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
        EnsureNotDisposed();

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

    public void Dispose()
    {
        StopProcess();
        _isDisposed = true;
    }

    private void Init()
    {
        EnsureNotDisposed();

        if (_processStartInfo == null!)
        {
            _processStartInfo = new ProcessStartInfo(_commandText!)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            if (!string.IsNullOrWhiteSpace(_args))
                _processStartInfo.Arguments = _args;

            // Copy current env variables to new process
            var envVars = Environment.GetEnvironmentVariables();
            foreach (string key in envVars.Keys)
            {
                _processStartInfo.EnvironmentVariables[key] = envVars[key]?.ToString();
            }
        }

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

    private void EnsureNotDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ProcessHandler), "The process handler is disposed.");
        }
    }
}
