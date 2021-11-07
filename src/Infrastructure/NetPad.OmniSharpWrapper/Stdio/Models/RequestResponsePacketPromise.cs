using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NetPad.OmniSharpWrapper.Stdio.Models
{
    public class RequestResponsePacketPromise
    {
        private readonly TaskCompletionSource<JToken> _taskCompletionSource;
        
        public RequestResponsePacketPromise(RequestPacket requestPacket)
        {
            RequestPacket = requestPacket;

            _taskCompletionSource = new TaskCompletionSource<JToken>();

            Task = _taskCompletionSource.Task;
        }
        
        public RequestPacket RequestPacket { get; }
        public JToken? Response { get; private set; }

        public Task<JToken> Task { get; }
        
        public void SetResponse(JToken response)
        {
            Response = response;
            _taskCompletionSource.SetResult(response);
        }
    }
}