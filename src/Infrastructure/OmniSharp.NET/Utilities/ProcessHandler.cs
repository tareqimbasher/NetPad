using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OmniSharp.Utilities
{
    internal class ProcessHandler : IDisposable
    {
        private readonly string? _commandText;
        private readonly string? _args;
        private Process? _process;
        private ProcessIOHandler? _processIOHandler;

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

            _process = new Process { StartInfo = startInfo };
                
            if (!string.IsNullOrWhiteSpace(_args))
                _process.StartInfo.Arguments = _args;
                
            _processIOHandler = new ProcessIOHandler(_process);
                
            if (!_process.Start())
                return Task.FromResult(false);
            
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            if (waitForExit)
                _process.WaitForExit();

            return Task.FromResult(true);
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