using Microsoft.Extensions.Logging;
using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;
using NetPad.Common;

namespace NetPad.Apps.Shells.Electron.UiInterop;

public class ElectronIpcService(ILogger<ElectronIpcService> logger) : IIpcService
{
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
            logger.LogError(ex, "Error sending message on channel: {Channel}", channel);
        }

        return Task.CompletedTask;
    }

    public Task<TResponse?> SendAndReceiveAsync<TResponse>(Command<TResponse> message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
