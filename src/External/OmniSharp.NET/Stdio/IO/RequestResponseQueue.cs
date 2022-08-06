using System.Collections.Generic;
using System.Threading.Tasks;

namespace OmniSharp.Stdio.IO
{
    public class RequestResponseQueue
    {
        private readonly Dictionary<int, RequestResponsePacketPromise> _promises;

        public RequestResponseQueue()
        {
            _promises = new Dictionary<int, RequestResponsePacketPromise>();
        }

        public Task<ResponseJsonObject> Enqueue(RequestPacket requestPacket)
        {
            var promise = new RequestResponsePacketPromise(requestPacket);
            _promises.Add(requestPacket.Seq, promise);
            return promise.Task;
        }

        public void HandleResponse(ResponseJsonObject response)
        {
            int requestSequence = response.RequestSequence();

            if (!_promises.TryGetValue(requestSequence, out var promise)) return;
            _promises.Remove(requestSequence);
            promise.SetResponse(response);
        }
    }
}
