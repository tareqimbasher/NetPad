using System.IO;

namespace NetPad.IO.IPC.Stdio;

internal class Input(TextReader reader, Action<string> onInputReceived)
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
        Task.Factory.StartNew(async () =>
        {
            while (!_listenForInput.IsCancellationRequested)
            {
                string? input = await reader.ReadLineAsync();

                if (input == null)
                {
                    continue;
                }

                try
                {
                    _ = Task.Run(() => onInputReceived(input));
                }
                catch
                {
                    // Should be handled in message handlers
                }
            }
        }, _listenForInput.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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
