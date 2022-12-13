using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OmniSharp.Utilities
{
    internal class ProcessStartResult
    {
        public ProcessStartResult(bool success, Task waitForExitTask)
        {
            Success = success;
            WaitForExitTask = waitForExitTask;
        }

        public bool Success { get; }
        public Task WaitForExitTask { get; }
    }

    internal sealed class ProcessHandler : IDisposable
    {
        private readonly string? _commandText;
        private readonly string? _args;
        private Process? _process;
        private Task? _processStartTask;
        private ProcessStartInfo? _processStartInfo;

        public ProcessHandler(string commandText)
        {
            _commandText = commandText ?? throw new ArgumentNullException(nameof(commandText));
        }

        public ProcessHandler(string commandText, string args) : this(commandText)
        {
            _args = args ?? throw new ArgumentNullException(nameof(args));
        }

        public Process Process => _process ??
                                  throw new InvalidOperationException(
                                      "Process has not been started yet");

        public ProcessIO? IO { get; private set; }

        public ProcessStartResult StartProcess(ProcessStartInfo? processStartInfo = null)
        {
            if (_process != null && _process.IsProcessRunning())
                throw new InvalidOperationException(
                    $"Process is already started and has not exited yet. Process PID: {_process.Id}.");

            if (processStartInfo != null)
            {
                _processStartInfo = processStartInfo;
            }
            else if (_processStartInfo == null)
            {
                _processStartInfo = new ProcessStartInfo(_commandText)
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


            _process = new Process
            {
                StartInfo = _processStartInfo,
                EnableRaisingEvents = true
            };

            IO = new ProcessIO(_process);

            _process.Start();

            // We have to wait for the process or otherwise it exists shortly after its spawned
            _processStartTask = Task.Run(() => { _process.WaitForExit(); });

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            bool started = _process.IsProcessRunning();

            return new ProcessStartResult(started, _processStartTask);
        }

        public void StopProcess()
        {
            if (_process != null && _process.IsProcessRunning())
            {
                _process.Kill();
            }

            _processStartTask = null;
            _process?.Dispose();
        }

        public void Restart()
        {
            if (_process?.IsProcessRunning() == true)
            {
                StopProcess();
            }

            StartProcess(_processStartInfo);
        }

        public void Dispose()
        {
            StopProcess();
        }
    }
}
