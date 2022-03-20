using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NetPad.Utilities;

namespace NetPad.IO
{
    public class ProcessHandler : IDisposable
    {
        private Process? _process;
        private ProcessIOHandler? _processIOHandler;
        private bool _ranOnce;

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

        public void Init()
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
            _process.EnableRaisingEvents = true;

            if (!string.IsNullOrWhiteSpace(Args))
                _process.StartInfo.Arguments = Args;

            _processIOHandler = new ProcessIOHandler(_process);
        }

        public async Task<bool> RunAsync(bool waitForExit = true)
        {
            if (_process == null)
                throw new InvalidOperationException($"Process is not initialized.");

            var success = _process.Start();
            if (!success)
                return false;

            if (!_ranOnce)
            {
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
            }

            _ranOnce = true;

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
