using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using NetPad.CQs;
using JsonSerializer = NetPad.Common.JsonSerializer;

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

        public async Task<TResponse?> SendAndReceiveAsync<TResponse>(Command<TResponse> message)
        {
            await SendAsync(message.GetType().Name, message);

            var promise = new ResponsePromise<TResponse>();
            TransactionQueue.Enqueue(message.Id, promise);
            return await promise.Task;
        }
    }

    public class IpcHub : Hub
    {
        public void Respond(Guid messageId, JsonElement response)
        {
            TransactionQueue.ResponseReceived(messageId, response);
        }
    }

    internal static class TransactionQueue
    {
        private static readonly Dictionary<Guid, ResponsePromise> _promises;

        static TransactionQueue()
        {
            _promises = new Dictionary<Guid, ResponsePromise>();
        }

        public static void Enqueue(Guid messageId, ResponsePromise promise)
        {
            _promises.Add(messageId, promise);
        }

        public static void ResponseReceived(Guid messageId, JsonElement response)
        {
            if (!_promises.TryGetValue(messageId, out var promise))
                return;

            _promises.Remove(messageId);
            promise.SetResponse(response);
        }
    }

    internal abstract class ResponsePromise
    {
        public abstract void SetResponse(JsonElement response);
    }

    internal class ResponsePromise<TResponse> : ResponsePromise
    {
        private readonly TaskCompletionSource<TResponse?> _taskCompletionSource;

        public ResponsePromise()
        {
            _taskCompletionSource = new TaskCompletionSource<TResponse?>();
            Task = _taskCompletionSource.Task;
        }

        // public TResponse? Response { get; private set; }

        public Task<TResponse?> Task { get; }

        public override void SetResponse(JsonElement response)
        {
            // Response = response;
            _taskCompletionSource.SetResult(response.Deserialize<TResponse>(JsonSerializer.DefaultOptions));
        }
    }
}
