using System.Threading.Tasks;

namespace OmniSharp.Stdio.IO
{
    public class RequestResponsePacketPromise
    {
        private readonly TaskCompletionSource<ResponseJsonObject> _taskCompletionSource;

        public RequestResponsePacketPromise(RequestPacket requestPacket)
        {
            RequestPacket = requestPacket;

            _taskCompletionSource = new TaskCompletionSource<ResponseJsonObject>(TaskCreationOptions.RunContinuationsAsynchronously);

            Task = _taskCompletionSource.Task;
        }

        public RequestPacket RequestPacket { get; }
        public ResponseJsonObject? Response { get; private set; }

        public Task<ResponseJsonObject> Task { get; }

        public void SetResponse(ResponseJsonObject response)
        {
            Response = response;
            _taskCompletionSource.SetResult(response);
        }

        public void Cancel()
        {
            _taskCompletionSource.TrySetCanceled();
        }
    }
}
