using System.IO;

namespace NetPad.IO.IPC.Stdio;

internal class StdioInputChannel(TextReader reader, Action<string> onInputReceived)
{
    private CancellationTokenSource? _listenForInput = new();

    public void StartListening()
    {
        if (_listenForInput?.IsCancellationRequested == true)
        {
            return;
        }

        _listenForInput = new();

        // This thread will exit when cancellation is requested or the TextReader stream is closed.
        Task.Factory.StartNew(cancellationToken =>
        {
            var ct = (CancellationToken)cancellationToken!;
            while (!ct.IsCancellationRequested)
            {
                var input = reader.ReadLine();
                if (input == null)
                {
                    continue;
                }

                // Execute handler on thread pool
                _ = Task.Run(() =>
                {
                    try
                    {
                        onInputReceived(input);
                    }
                    catch
                    {
                        // Should be handled in message handlers
                    }
                });
            }
        }, _listenForInput.Token, _listenForInput.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void CancelListening()
    {
        if (_listenForInput != null)
        {
            _listenForInput.Cancel();
            _listenForInput.Dispose();
            _listenForInput = null;
        }
    }
}
