using System.Text.Json;
using System.Threading.Tasks;
using ElectronNET.API;
using NetPad.Common;
using NetPad.Services;

namespace NetPad.UiInterop
{
    public class ElectronIpcService : IIpcService
    {
        public async Task SendAsync<TMessage>(TMessage message) where TMessage : class
        {
            await SendAsync(typeof(TMessage).Name, message);
        }

        public Task SendAsync(string channel, object? message)
        {
            Electron.IpcMain.Send(
                ElectronUtil.MainWindow,
                channel,
                JsonSerializer.Serialize(message, JsonSerialization.DefaultOptions));

            return Task.CompletedTask;
        }
    }
}
