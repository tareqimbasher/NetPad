using Microsoft.AspNetCore.SignalR;
using NetPad.CQs;

namespace NetPad.UiInterop;

/// <summary>
/// An IPC service that uses SignalR to communicate with external processes/clients.
/// </summary>
public class SignalRIpcService : IIpcService
{
    private readonly IHubContext<IpcHub> _hubContext;

    public SignalRIpcService(IHubContext<IpcHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class
    {
        await SendAsync(typeof(TMessage).Name, message, cancellationToken);
    }

    public async Task SendAsync(string channel, object? message, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return;

        await _hubContext.Clients.All.SendAsync(
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
