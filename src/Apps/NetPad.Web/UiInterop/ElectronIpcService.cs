using System;
using System.Threading.Tasks;
using ElectronNET.API;
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
            try
            {
                Electron.IpcMain.Send(
                    ElectronUtil.MainWindow,
                    channel,
                    message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return Task.CompletedTask;
        }
    }
}
