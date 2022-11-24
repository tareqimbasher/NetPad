using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace OmniSharp.Stdio.IO
{
    internal class RequestResponseQueue
    {
        private readonly ConcurrentDictionary<int, RequestResponsePacketPromise> _promises;

        public RequestResponseQueue()
        {
            _promises = new ConcurrentDictionary<int, RequestResponsePacketPromise>();
        }

        public Task<ResponseJsonObject> Enqueue(RequestPacket requestPacket, CancellationToken cancellationToken)
        {
            var promise = new RequestResponsePacketPromise(requestPacket);

            int requestSequence = requestPacket.Seq;

            if (!_promises.TryAdd(requestSequence, promise))
            {
                bool exists = _promises.ContainsKey(requestPacket.Seq);
                throw new Exception($"Could not add request to queue. Key already exists? {exists}");
            }

            cancellationToken.Register(() => Cancel(requestPacket));

            return promise.Task;
        }

        public void HandleResponse(ResponseJsonObject response)
        {
            int requestSequence = response.RequestSequence();

            if (!_promises.TryRemove(requestSequence, out var promise)) return;
            promise.SetResponse(response);
        }

        public void WaitingForResponseFailed(RequestPacket requestPacket)
        {
            Cancel(requestPacket);
        }

        private void Cancel(RequestPacket requestPacket)
        {
            if (!_promises.TryRemove(requestPacket.Seq, out var promise))
                return;

            promise.Cancel();
        }
    }
}
