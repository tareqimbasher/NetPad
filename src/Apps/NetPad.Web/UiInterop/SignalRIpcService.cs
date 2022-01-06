using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace NetPad.UiInterop
{
    public class SignalRIpcService : IIpcService
    {
        private readonly IHubContext<IpcHub> _hubContext;

        public SignalRIpcService(IHubContext<IpcHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendAsync<TMessage>(TMessage message) where TMessage : class
        {
            await SendAsync(typeof(TMessage).Name, message);
        }

        public async Task SendAsync(string channel, object? message)
        {
            await _hubContext.Clients.All.SendAsync(
                channel,
                message);
        }
    }

    public class IpcHub : Hub
    {
    }
}
