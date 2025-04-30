using System.Diagnostics.CodeAnalysis;
using System.IO;
using NetPad.Common;

namespace NetPad.IO.IPC.Stdio;

/// <summary>
/// Used to communicate between two processes over standard IO (STDIN/STDOUT).
/// </summary>
public class StdioIpcGateway<TMessage>(TextWriter sendChannel) : IDisposable where TMessage : class
{
    private readonly Output _output = new(sendChannel);
    private Input? _input;

    public void Listen(
        TextReader receiveChannel,
        Action<TMessage> onMessageReceived,
        Action<string>? onNonMessageReceived = null)
    {
        _input = new Input(receiveChannel, input =>
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

        _input.StartListening();
    }

    public void Send(TMessage message)
    {
        _output.Write(message);
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
        _input?.CancelListening();
    }
}
