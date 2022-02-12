using System.Threading.Tasks;

namespace NetPad.UiInterop
{
    public interface IIpcService
    {
        Task SendAsync<TMessage>(TMessage message) where TMessage : class;
        Task SendAsync(string channel, object? message);
    }
}
