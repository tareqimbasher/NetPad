using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Events;

namespace NetPad.IO.IPC.Stdio;

/// <summary>
/// A messaging interface between two processes that sends and receives messages using standard input/output streams.
/// </summary>
/// <param name="sendChannel">
/// A text writer that will be written to when a message is sent.
/// <remarks>
/// This is typically the STDOUT of the current process, or the STDIN of another process.
/// </remarks>
/// </param>
/// <param name="resolver"></param>
/// <param name="logger"></param>
public class StdioIpcGateway(TextWriter sendChannel, ITypeNameResolver? resolver = null, ILogger? logger = null)
    : IDisposable
{
    private interface IHandler
    {
        void Invoke(object message);
    }

    private sealed class Handler<T>(Action<T> action) : IHandler
    {
        public void Invoke(object message)
        {
            action((T)message);
        }
    }

    private readonly StdioOutputChannel _outputChannel = new(sendChannel);
    private StdioInputChannel? _inputChannel;
    private readonly ITypeNameResolver _resolver = resolver ?? new FullNameTypeResolver();
    private readonly Dictionary<Type, List<IHandler>> _handlers = new();
    private readonly object _handlersLock = new();
    private long _seq;

    /// <summary>
    /// Starts listening to messages written to the specified text reader.
    /// </summary>
    /// <param name="receiveChannel">
    /// The text reader to read received messages from.
    /// <remarks>
    /// This is usually the STDIN of the current process, or the STDOUT of another process.
    /// </remarks>
    /// </param>
    /// <param name="onNonMessageReceived">
    /// An action to execute when data is received and cannot be parsed correctly.
    /// </param>
    public void Listen(TextReader receiveChannel, Action<string>? onNonMessageReceived = null)
    {
        ArgumentNullException.ThrowIfNull(receiveChannel);

        if (_inputChannel != null)
        {
            throw new InvalidOperationException("Already listening");
        }

        _inputChannel = new StdioInputChannel(receiveChannel, input =>
        {
            try
            {
                if (!TryParseEnvelope(input, out var envelope) ||
                    !TryParseMessage(envelope.Type, envelope.Data, out var messageType, out var message))
                {
                    onNonMessageReceived?.Invoke(input);
                    return;
                }

                Dispatch(messageType, message);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Error handling input received");
            }
        });

        _inputChannel.StartListening();
    }

    /// <summary>
    /// Registers a handler to be invoked when a message of type <typeparamref name="T"/> is received.
    /// </summary>
    public void On<T>(Action<T> handler) where T : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_handlersLock)
        {
            if (!_handlers.TryGetValue(typeof(T), out var handlers))
            {
                handlers = [];
                _handlers[typeof(T)] = handlers;
            }

            handlers.Add(new Handler<T>(handler));
        }
    }

    /// <summary>
    /// Unregisters a handler that was previously registered with <see cref="On{T}"/>.
    /// </summary>
    public void Off<T>(Action<T> handler) where T : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_handlersLock)
        {
            if (!_handlers.TryGetValue(typeof(T), out var handlers))
            {
                return;
            }

            // remove the specific wrapper instance
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                if (handlers[i] is Handler<T> h && ReferenceEquals(h, handlers[i]))
                {
                    handlers.RemoveAt(i);
                }
            }

            if (handlers.Count == 0)
            {
                _handlers.Remove(typeof(T));
            }
        }
    }

    /// <summary>
    /// Registers a handler and returns an <see cref="IDisposable"/> that, when disposed, unregisters it.
    /// </summary>
    public IDisposable Subscribe<T>(Action<T> handler) where T : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        On(handler);
        return new DisposableToken(() => Off(handler));
    }


    /// <summary>
    /// Sends a message on the configured send channel.
    /// </summary>
    public void Send(object message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var seq = Interlocked.Increment(ref _seq);
        var typeName = _resolver.GetName(message.GetType());
        var data = JsonSerializer.Serialize(message);
        _outputChannel.Write(new StdioIpcEnvelope(seq, typeName, data));
    }

    /// <summary>
    /// Executes any handlers associated with the specified message type.
    /// </summary>
    public void ExecuteHandlers<T>(T message) where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        Dispatch(typeof(T), message);
    }

    private void Dispatch(Type messageType, object message)
    {
        List<IHandler>? handlers;
        lock (_handlersLock)
        {
            if (!_handlers.TryGetValue(messageType, out var messageHandlers))
            {
                return;
            }

            handlers = [..messageHandlers];
        }

        foreach (var h in handlers)
        {
            try
            {
                h.Invoke(message);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error invoking handler for {MessageType}", messageType);
            }
        }
    }

    private static bool TryParseEnvelope(string input, [NotNullWhen(true)] out StdioIpcEnvelope? envelope)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            envelope = null;
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<StdioIpcEnvelope>(input);
            if (parsed != null)
            {
                envelope = parsed;
                return true;
            }
        }
        catch
        {
            // ignore
        }

        envelope = null;
        return false;
    }

    private bool TryParseMessage(
        string type,
        string data,
        [NotNullWhen(true)] out Type? messageType,
        [NotNullWhen(true)] out object? message)
    {
        messageType = null;
        message = null;

        messageType = _resolver.Resolve(type);
        if (messageType == null)
        {
            return false;
        }

        try
        {
            message = JsonSerializer.Deserialize(data, messageType);
        }
        catch
        {
            return false;
        }

        return message != null;
    }

    public void Dispose()
    {
        _inputChannel?.CancelListening();
        lock (_handlersLock)
        {
            _handlers.Clear();
        }
    }
}
