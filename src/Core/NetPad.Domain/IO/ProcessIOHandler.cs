using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NetPad.Utilities;

namespace NetPad.IO
{
    public class ProcessIOHandler : IDisposable
    {
        private readonly object _outputReceivedLock;
        private readonly object _errorReceivedLock;

        public ProcessIOHandler(Process process)
        {
            Process = process;

            _outputReceivedLock = new object();
            _errorReceivedLock = new object();
            OnOutputReceivedHandlers = new List<Func<string, Task>>();
            OnErrorReceivedHandlers = new List<Func<string, Task>>();

            Process.OutputDataReceived += OutputReceived;
            Process.ErrorDataReceived += ErrorReceived;
        }

        public Process Process { get; private set; }

        public List<Func<string, Task>> OnOutputReceivedHandlers { get; }
        public List<Func<string, Task>> OnErrorReceivedHandlers { get; }

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
            Process.OutputDataReceived -= OutputReceived;
            Process.ErrorDataReceived -= ErrorReceived;
            OnOutputReceivedHandlers.Clear();
            OnErrorReceivedHandlers.Clear();
            Process = null!;
        }
    }
}
