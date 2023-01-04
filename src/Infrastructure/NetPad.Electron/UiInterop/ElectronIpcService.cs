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

    public async Task SendAsync<TMessage>(TMessage message) where TMessage : class
    {
        await SendAsync(typeof(TMessage).Name, message);
    }

    public Task SendAsync(string channel, object? message)
    {
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

    public Task<TResponse?> SendAndReceiveAsync<TResponse>(Command<TResponse> message)
    {
        throw new PlatformNotSupportedException();
    }
}
