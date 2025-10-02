using System.IO;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace NetPad.IO.IPC.Stdio;

internal sealed class StdioInputChannel(TextReader reader, Action<string> onInputReceived, ILogger? logger = null)
{
    private readonly TextReader _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    private readonly Action<string> _onInputReceived =
        onInputReceived ?? throw new ArgumentNullException(nameof(onInputReceived));

    private Task? _listenerPump;
    private CancellationTokenSource? _cts;
    private Channel<string>? _channel;
    private Task[]? _workers;

    public void StartListening()
    {
        logger?.LogTrace("Start listening");
        if (_cts is { IsCancellationRequested: false } || _listenerPump is { IsCompleted: false })
        {
            logger?.LogTrace("Already listening");
            throw new InvalidOperationException("Already listening");
        }

        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(capacity: 1024)
        {
            SingleWriter = true,
            SingleReader = false,
            // Applies backpressure to writers
            FullMode = BoundedChannelFullMode.Wait
        });

        _listenerPump = Task.Factory.StartNew(() =>
        {
            logger?.LogTrace("Listen pump started");
            var writer = _channel.Writer;

            while (!token.IsCancellationRequested)
            {
                logger?.LogTrace("Waiting for readLine()");
                string? line;
                try
                {
                    line = _reader.ReadLine();
                }
                catch
                {
                    logger?.LogTrace("Reader faulted or disposed while waiting for readLine()");
                    break;
                }

                if (line is null || token.IsCancellationRequested)
                {
                    logger?.LogTrace("readLine() returned EOF or reader closed");
                    break;
                }

                try
                {
                    logger?.LogTrace("Queueing received message");
                    while (!writer.TryWrite(line))
                    {
                        // OK to use sync await here
                        writer.WaitToWriteAsync(token).GetAwaiter().GetResult();
                    }
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Error occurred trying to write received message to queue");
                }
            }

            logger?.LogTrace("Listen pump stopped");
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        const int degree = 4;
        _workers = Enumerable.Range(0, degree).Select(_ => Task.Run(async () =>
        {
            try
            {
                var readerChannel = _channel.Reader;
                while (await readerChannel.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (readerChannel.TryRead(out var msg))
                    {
                        try
                        {
                            _onInputReceived(msg);
                        }
                        catch
                        {
                            // Should be handled in message handlers
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Error occurred in main worker");
            }
        }, token)).ToArray();
    }

    public void CancelListening()
    {
        logger?.LogTrace("Cancel listening");

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
        _listenerPump = null;

        Try.Run(() => Task.WhenAll(_workers ?? []).Wait(TimeSpan.FromSeconds(2)));
        _workers = null;
        _channel = null;
    }
}
