using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.OmniSharpWrapper.Stdio.Models;
using Newtonsoft.Json.Linq;

namespace NetPad.OmniSharpWrapper.Stdio
{
    public class RequestResponseQueue
    {
        private readonly ILogger<RequestResponseQueue> _logger;
        private readonly Dictionary<int, RequestResponsePacketPromise> _promises;

        public RequestResponseQueue(ILogger<RequestResponseQueue> logger)
        {
            _logger = logger;
            _promises = new Dictionary<int, RequestResponsePacketPromise>();
        }

        public Task<JToken> Enqueue(RequestPacket requestPacket)
        {
            var promise = new RequestResponsePacketPromise(requestPacket);
            _promises.Add(requestPacket.Seq, promise);
            return promise.Task;
        }

        public void HandleResponse(int requestSeq, string? command, JToken response)
        {
            if (!_promises.TryGetValue(requestSeq, out var promise))
            {
                _logger.LogWarning("Received response for '{Command}' but could not find request", command);
                return;
            }

            promise.SetResponse(response);
            _promises.Remove(requestSeq);
        }
    }
}
