using NetPad.CQs;
using NetPad.Common;
using NetPad.UiInterop;

namespace NetPad.Electron.UiInterop
{
    public class ElectronIpcService : IIpcService
    {
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
                Console.WriteLine(ex);
            }
            return Task.CompletedTask;
        }

        public Task<TResponse?> SendAndReceiveAsync<TResponse>(Command<TResponse> message)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
