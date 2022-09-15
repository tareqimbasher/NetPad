using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace OmniSharp.Stdio.IO
{
    public class RequestResponseQueue
    {
        private readonly ConcurrentDictionary<int, RequestResponsePacketPromise> _promises;

        public RequestResponseQueue()
        {
            _promises = new ConcurrentDictionary<int, RequestResponsePacketPromise>();
        }

        public Task<ResponseJsonObject> Enqueue(RequestPacket requestPacket)
        {
            var promise = new RequestResponsePacketPromise(requestPacket);

            if (!_promises.TryAdd(requestPacket.Seq, promise))
            {
                bool exists = _promises.ContainsKey(requestPacket.Seq);
                throw new Exception($"Could not add request to queue. Key already exists? {exists}");
            }

            return promise.Task;
        }

        public void HandleResponse(ResponseJsonObject response)
        {
            int requestSequence = response.RequestSequence();

            if (!_promises.TryRemove(requestSequence, out var promise)) return;
            promise.SetResponse(response);
        }
    }
}
