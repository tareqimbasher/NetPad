using System.Diagnostics.CodeAnalysis;
using System.IO;
using NetPad.Common;

namespace NetPad.IO.IPC.Stdio;

/// <summary>
/// Used for two-way communication between two processes over standard IO (STDIN/STDOUT).
/// </summary>
/// <param name="sendChannel">The IO channel to use to send messages.</param>
/// <typeparam name="TMessage">The type of messages both processes agree to send and receive.</typeparam>
public class StdioIpcGateway<TMessage>(TextWriter sendChannel) : IDisposable where TMessage : class
{
    private readonly StdioOutputChannel _outputChannel = new(sendChannel);
    private StdioInputChannel? _inputChannel;

    /// <summary>
    /// Start listening for messages on the specified IO channel.
    /// </summary>
    /// <param name="receiveChannel">The IO stream to listen for messages on.</param>
    /// <param name="onMessageReceived">An action to execute when a message is received.</param>
    /// <param name="onNonMessageReceived">An action to execute when data is received and cannot be parsed into a
    /// message of type <typeparamref name="TMessage"/>.</param>
    public void Listen(
        TextReader receiveChannel,
        Action<TMessage> onMessageReceived,
        Action<string>? onNonMessageReceived = null)
    {
        _inputChannel = new StdioInputChannel(receiveChannel, input =>
        {
            if (TryParse(input, out TMessage? message))
            {
                onMessageReceived(message);
            }
            else
            {
                onNonMessageReceived?.Invoke(input);
            }
        });

        _inputChannel.StartListening();
    }

    /// <summary>
    /// Sends a message on the configured send channel.
    /// </summary>
    public void Send(TMessage message)
    {
        _outputChannel.Write(message);
    }

    private static bool TryParse(string input, [NotNullWhen(true)] out TMessage? message)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            message = null;
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<TMessage>(input);

            if (parsed != null)
            {
                message = parsed;
                return true;
            }
        }
        catch
        {
            // ignore
        }

        message = null;
        return false;
    }

    public void Dispose()
    {
        _inputChannel?.CancelListening();
    }
}
