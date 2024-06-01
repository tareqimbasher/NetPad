using NetPad.Apps.CQs;

namespace NetPad.Apps.UiInterop;

/// <summary>
/// An inter-process communication (IPC) service used to communicate with external processes/clients.
/// </summary>
public interface IIpcService
{
    Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class;
    Task SendAsync(string channel, object? message, CancellationToken cancellationToken = default);
    Task<TResponse?> SendAndReceiveAsync<TResponse>(Command<TResponse> message, CancellationToken cancellationToken = default);
}
