using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OmniSharp.Utilities
{
    public class ProcessIOHandler : IDisposable
    {
        public ProcessIOHandler(Process process)
        {
            Process = process;

            OnOutputReceivedHandlers = new List<Func<string, Task>>();
            OnErrorReceivedHandlers = new List<Func<string, Task>>();

            Process.OutputDataReceived += OutputReceived;
            Process.ErrorDataReceived += ErrorReceived;
        }

        public Process Process { get; private set; }

        public StreamWriter StandardInput => Process.StandardInput;
        public List<Func<string, Task>> OnOutputReceivedHandlers { get; }
        public List<Func<string, Task>> OnErrorReceivedHandlers { get; }

        private void OutputReceived(object? sender, DataReceivedEventArgs ev)
        {
            if (ev.Data == null)
                return;

            foreach (var handler in OnOutputReceivedHandlers.ToArray())
            {
                AsyncHelpers.RunSync(() => handler(ev.Data));
            }
        }

        private void ErrorReceived(object? sender, DataReceivedEventArgs ev)
        {
            if (ev.Data == null)
                return;

            foreach (var handler in OnErrorReceivedHandlers.ToArray())
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
