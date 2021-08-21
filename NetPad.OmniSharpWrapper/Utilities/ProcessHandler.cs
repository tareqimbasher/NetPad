using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace NetPad.OmniSharpWrapper.Utilities
{
    internal class ProcessHandler : IDisposable
    {
        private Process? _process;
        private ProcessIOHandler? _processIOHandler;

        public ProcessHandler(string commandText)
        {
            CommandText = commandText;
        }

        public ProcessHandler(string commandText, string args) : this(commandText)
        {
            Args = args;
        }

        public string CommandText { get; }
        public string? Args { get; }

        public Process? Process => _process;
        public ProcessIOHandler? ProcessIO => _processIOHandler;

        public async Task<bool> RunAsync(bool waitForExit = true)
        {
            if (_process != null && _process.HasExited != true)
                throw new InvalidOperationException($"Process is still running and has not exited yet. Process PID: {_process?.Id}.");

            var startInfo = new ProcessStartInfo(CommandText)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _process = new Process { StartInfo = startInfo};

            if (!string.IsNullOrWhiteSpace(Args))
                _process.StartInfo.Arguments = Args;

            _processIOHandler = new ProcessIOHandler(_process);

            if (!_process.Start())
                return false;

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            if (!waitForExit) return true;
            
            await _process.WaitForExitAsync();

            return true;
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