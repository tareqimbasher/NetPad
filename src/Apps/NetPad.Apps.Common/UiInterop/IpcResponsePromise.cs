using System.Text.Json;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Apps.UiInterop;

internal abstract class IpcResponsePromise
{
    public abstract void SetResponse(JsonElement response);
}

internal class ResponsePromise<TResponse> : IpcResponsePromise
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
