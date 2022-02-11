using System.Threading.Tasks;

namespace OmniSharp.Stdio.IO
{
    public class RequestResponsePacketPromise
    {
        private readonly TaskCompletionSource<ResponseJObject> _taskCompletionSource;
        
        public RequestResponsePacketPromise(RequestPacket requestPacket)
        {
            RequestPacket = requestPacket;

            _taskCompletionSource = new TaskCompletionSource<ResponseJObject>();

            Task = _taskCompletionSource.Task;
        }
        
        public RequestPacket RequestPacket { get; }
        public ResponseJObject? Response { get; private set; }

        public Task<ResponseJObject> Task { get; }
        
        public void SetResponse(ResponseJObject response)
        {
            Response = response;
            _taskCompletionSource.SetResult(response);
        }
    }
}