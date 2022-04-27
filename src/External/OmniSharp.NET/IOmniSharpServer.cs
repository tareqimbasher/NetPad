using System;
using System.Threading.Tasks;

namespace OmniSharp
{
    public interface IOmniSharpServer : IDisposable
    {
        Task StartAsync();
        Task StopAsync();

        Task Send(object request);

        Task<TResponse?> Send<TResponse>(object request)
            where TResponse : class;
    }
}
