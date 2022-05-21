using System;
using System.Threading.Tasks;

namespace OmniSharp
{
    /// <summary>
    /// An OmniSharp server instance.
    /// </summary>
    public interface IOmniSharpServer : IDisposable
    {
        /// <summary>
        /// Starts the server.
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stops the server.
        /// </summary>
        /// <returns></returns>
        Task StopAsync();

        /// <summary>
        /// Sends a request to the server with no response.
        /// </summary>
        /// <param name="request">The request to send. Must be an OmniSharp request model from OmniSharp.Models.</param>
        Task Send(object request);

        /// <summary>
        /// Sends a request to the server and returns the server response.
        /// </summary>
        /// <param name="request">The request to send. Must be an OmniSharp request model from OmniSharp.Models.</param>
        /// <typeparam name="TResponse">The type of the expected response. Should be an appropriate OmniSharp response model from OmniSharp.Models.</typeparam>
        Task<TResponse?> Send<TResponse>(object request)
            where TResponse : class;
    }
}
