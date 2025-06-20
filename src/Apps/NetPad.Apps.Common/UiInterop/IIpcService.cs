using NetPad.Apps.CQs;

namespace NetPad.Apps.UiInterop;

/// <summary>
/// A service used to send messages to external clients (or processes); ie. inter-process communication (IPC).
/// </summary>
public interface IIpcService
{
    /// <summary>
    /// Sends a message on the specified channel to connected clients.
    /// </summary>
    /// <param name="channel">The channel to send the message on.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken"></param>
    Task SendAsync(string channel, object? message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to connected clients. The channel the message is sent on is inferred from the message type.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken"></param>
    Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class;

    /// <summary>
    /// Sends a message to connected clients and waits for the client to respond before returning.
    /// The channel the message is sent on is inferred from the message type.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TResponse">The type of the expected response.</typeparam>
    /// <returns>The client's response.</returns>
    Task<TResponse?> SendAndReceiveAsync<TResponse>(
        Command<TResponse> message,
        CancellationToken cancellationToken = default);
}
