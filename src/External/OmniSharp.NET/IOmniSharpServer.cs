using System;
using System.Collections.Generic;
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
        /// <param name="request">The request to send. Must be an OmniSharp request model from OmniSharp.Models nuget package.</param>
        Task SendAsync(object request);

        /// <summary>
        /// Sends a request to the server and returns the server response.
        /// </summary>
        /// <param name="request">The request to send. Must be an OmniSharp request model from OmniSharp.Models nuget package.</param>
        /// <typeparam name="TResponse">The type of the expected response. Should be an appropriate OmniSharp response model from OmniSharp.Models nuget package.</typeparam>
        Task<TResponse?> SendAsync<TResponse>(object request) where TResponse : class;

        /// <summary>
        /// Sends a collection of requests to the server with no response.
        /// </summary>
        /// <param name="requests">The request collection to send. Each item must be an OmniSharp request model from OmniSharp.Models nuget package.</param>
        /// <typeparam name="TRequest">The type each request.</typeparam>
        Task SendAsync<TRequest>(IEnumerable<TRequest> requests);

        /// <summary>
        /// Sends a collection of requests to the server and returns the server response.
        /// </summary>
        /// <param name="requests">The request collection to send. Each item must be an OmniSharp request model from OmniSharp.Models nuget package.</param>
        /// <typeparam name="TRequest">The type each request.</typeparam>
        /// <typeparam name="TResponse">The type of the expected response. Should be an appropriate OmniSharp response model from OmniSharp.Models nuget package.</typeparam>
        Task<TResponse?> SendAsync<TRequest, TResponse>(IEnumerable<TRequest> requests) where TResponse : class;

        /// <summary>
        /// Sends a request to the server and returns the server response
        /// </summary>
        /// <param name="endpointName">The OmniSharp endpoint.</param>
        /// <param name="request">The request to send.</param>
        /// <typeparam name="TResponse">The type of the expected response. Should be an appropriate OmniSharp response model from OmniSharp.Models nuget package or <see cref="NoResponse"/>.</typeparam>
        Task<TResponse?> SendAsync<TResponse>(string endpointName, object request) where TResponse : class;
    }
}
