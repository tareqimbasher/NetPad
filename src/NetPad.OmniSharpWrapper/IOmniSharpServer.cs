using System;
using System.Threading.Tasks;
using OmniSharp.Models;

namespace NetPad.OmniSharpWrapper
{
    public interface IOmniSharpServer : IDisposable
    {
        Task StartAsync();
        Task StopAsync();

        Task<TResponse> Send<TResponse>(object request)
            where TResponse : class;
    }
}