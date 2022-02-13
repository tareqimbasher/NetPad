using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace NetPad.Utilities
{
    public class ProcessHandler : IDisposable
    {
        private Process? _process;
        private readonly object _outputReceivedLock;
        private readonly object _errorReceivedLock;

        public ProcessHandler(string commandText)
        {
            _outputReceivedLock = new object();
            _errorReceivedLock = new object();
            OnOutputReceivedHandlers = new List<Func<string, Task>>();
            OnErrorReceivedHandlers = new List<Func<string, Task>>();

            CommandText = commandText;
        }

        public ProcessHandler(string commandText, string args) : this(commandText)
        {
            Args = args;
        }

        public string CommandText { get; }
        public string? Args { get; }
        public int ExitCode { get; set; }

        public Process? Process => _process;
        public StreamWriter? StandardInput => _process?.StandardInput;
        public List<Func<string, Task>> OnOutputReceivedHandlers { get; }
        public List<Func<string, Task>> OnErrorReceivedHandlers { get; }


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

            _process.OutputDataReceived += OutputReceived;
            _process.ErrorDataReceived += ErrorReceived;

            if (!_process.Start())
                return false;

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            if (!waitForExit) return true;

            await _process.WaitForExitAsync();
            ExitCode = _process.ExitCode;

            return true;
        }

        public void StopProcess()
        {
            if (_process == null || _process.HasExited) return;

            _process.Kill();
            _process.Dispose();
        }

        private void OutputReceived(object? sender, DataReceivedEventArgs ev)
        {
            if (ev.Data == null)
                return;

            foreach (var handler in OnOutputReceivedHandlers)
            {
                AsyncHelpers.RunSync(() => handler(ev.Data));
            }
        }

        private void ErrorReceived(object? sender, DataReceivedEventArgs ev)
        {
            if (ev.Data == null)
                return;

            foreach (var handler in OnErrorReceivedHandlers)
            {
                AsyncHelpers.RunSync(() => handler(ev.Data));
            }
        }

        public void Dispose()
        {
            if (_process != null)
            {
                _process.OutputDataReceived -= OutputReceived;
                _process.ErrorDataReceived -= ErrorReceived;
                StopProcess();
            }

            OnOutputReceivedHandlers.Clear();
            OnErrorReceivedHandlers.Clear();
        }
    }
}
