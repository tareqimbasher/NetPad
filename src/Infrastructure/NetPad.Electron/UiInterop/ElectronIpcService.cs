using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.CQs;
using NetPad.UiInterop;

namespace NetPad.Electron.UiInterop;

public class ElectronIpcService : IIpcService
{
    private readonly ILogger<ElectronIpcService> _logger;

    public ElectronIpcService(ILogger<ElectronIpcService> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class
    {
        await SendAsync(typeof(TMessage).Name, message, cancellationToken);
    }

    public Task SendAsync(string channel, object? message, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        try
        {
            ElectronNET.API.Electron.IpcMain.Send(
                ElectronUtil.MainWindow,
                channel,
                JsonSerializer.Serialize(message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message on channel: {Channel}", channel);
        }

        return Task.CompletedTask;
    }

    public Task<TResponse?> SendAndReceiveAsync<TResponse>(Command<TResponse> message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
