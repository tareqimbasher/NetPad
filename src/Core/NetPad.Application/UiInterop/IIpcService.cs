using NetPad.CQs;

namespace NetPad.UiInterop;

public interface IIpcService
{
    Task SendAsync<TMessage>(TMessage message) where TMessage : class;
    Task SendAsync(string channel, object? message);
    Task<TResponse?> SendAndReceiveAsync<TResponse>(Command<TResponse> message);
}
