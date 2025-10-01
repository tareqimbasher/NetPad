using System.IO;

namespace NetPad.IO.IPC.Stdio;

internal sealed class StdioInputChannel(TextReader reader, Action<string> onInputReceived)
{
    private readonly TextReader _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    private readonly Action<string> _onInputReceived =
        onInputReceived ?? throw new ArgumentNullException(nameof(onInputReceived));

    private CancellationTokenSource? _cts;
    private Task? _pump;

    public void StartListening()
    {
        if (_cts is { IsCancellationRequested: false } || _pump is { IsCompleted: false })
        {
            throw new InvalidOperationException("Already listening");
        }

        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _pump = Task.Factory.StartNew(() =>
        {
            while (!token.IsCancellationRequested)
            {
                string? line;
                try
                {
                    line = _reader.ReadLine();
                }
                catch (Exception)
                {
                    // Reader faulted or disposed
                    break;
                }

                if (line is null || token.IsCancellationRequested)
                {
                    // EOF/closed
                    break;
                }

                try
                {
                    _onInputReceived(line);
                }
                catch
                {
                    // Should be handled in message handlers
                }
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void CancelListening()
    {
        var cts = _cts;
        if (cts == null) return;

        try
        {
            cts.Cancel();
        }
        catch
        {
            // Ignore
        }

        cts.Dispose();
        _cts = null;
        _pump = null;
    }
}
