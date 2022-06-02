using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OmniSharp.Utilities
{
    internal class ProcessHandler : IDisposable
    {
        private readonly string? _commandText;
        private readonly string? _args;
        private Process? _process;
        private ProcessIOHandler? _processIOHandler;
        private Task? _task;

        public ProcessHandler(string commandText)
        {
            _commandText = commandText ?? throw new ArgumentNullException(nameof(commandText));
        }

        public ProcessHandler(string commandText, string args) : this(commandText)
        {
            _args = args ?? throw new ArgumentNullException(nameof(args));
        }

        public Process? Process => _process;
        public ProcessIOHandler? ProcessIO => _processIOHandler;

        public Task<bool> StartAsync(bool waitForExit = true)
        {
            if (_process != null && _process.IsProcessRunning())
                throw new InvalidOperationException($"Process is still running and has not exited yet. Process PID: {_process?.Id}.");

            var startInfo = new ProcessStartInfo(_commandText)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            if (!string.IsNullOrWhiteSpace(_args))
                startInfo.Arguments = _args;

            var envVars = Environment.GetEnvironmentVariables();
            foreach (string key in envVars.Keys)
            {
                startInfo.EnvironmentVariables[key] = envVars[key]?.ToString();
            }

            _process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            _processIOHandler = new ProcessIOHandler(_process);

            _processIOHandler.OnOutputReceivedHandlers.Add(o =>
            {
                File.AppendAllText("/home/tips/output.txt", o + "\n");
                return Task.CompletedTask;
            });

            _processIOHandler.OnErrorReceivedHandlers.Add(o =>
            {
                File.AppendAllText("/home/tips/errors.txt", o + "\n");
                return Task.CompletedTask;
            });

            if (!_process.Start())
                return Task.FromResult(false);

            // We have to wait for the process or otherwise it exists shortly after its spawned
            _task = Task.Run(() =>
            {
                _process.WaitForExit();
            });

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            return Task.FromResult(_process.IsProcessRunning());
        }

        public void StopProcess()
        {
            if (_process != null && _process.IsProcessRunning())
                _process.Kill();

            _process?.Dispose();
        }

        public void Dispose()
        {
            _processIOHandler?.Dispose();
            StopProcess();
        }
    }
}
