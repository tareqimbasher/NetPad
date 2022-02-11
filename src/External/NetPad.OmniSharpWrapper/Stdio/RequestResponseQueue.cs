using System.Collections.Generic;
using System.Threading.Tasks;
using NetPad.OmniSharpWrapper.Stdio.Models;
using Newtonsoft.Json.Linq;

namespace NetPad.OmniSharpWrapper.Stdio
{
    public class RequestResponseQueue
    {
        private readonly Dictionary<int, RequestResponsePacketPromise> _promises;

        public RequestResponseQueue()
        {
            _promises = new Dictionary<int, RequestResponsePacketPromise>();
        }
        
        public Task<JToken> Enqueue(RequestPacket requestPacket)
        {
            var promise = new RequestResponsePacketPromise(requestPacket);
            _promises.Add(requestPacket.Seq, promise);
            return promise.Task;
        }

        public void HandleResponse(int requestSeq, JToken response)
        {
            if (!_promises.TryGetValue(requestSeq, out var promise)) return;
            promise.SetResponse(response);
            _promises.Remove(requestSeq);
        }
    }
}