using Microsoft.AspNetCore.SignalR;
using NetPad.Apps.CQs;

namespace NetPad.Apps.UiInterop;

/// <summary>
/// An IPC service that uses SignalR to communicate with external processes/clients.
/// </summary>
public class SignalRIpcService(IHubContext<IpcHub> hubContext) : IIpcService
{
    public async Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class
    {
        await SendAsync(typeof(TMessage).Name, message, cancellationToken);
    }

    public async Task SendAsync(string channel, object? message, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return;

        await hubContext.Clients.All.SendAsync(
            channel,
            message,
            cancellationToken);
    }

    public async Task<TResponse?> SendAndReceiveAsync<TResponse>(Command<TResponse> message, CancellationToken cancellationToken = default)
    {
        await SendAsync(message.GetType().Name, message, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var promise = new ResponsePromise<TResponse>();
        IpcResponseQueue.Enqueue(message.Id, promise);
        return await promise.Task;
    }
}
