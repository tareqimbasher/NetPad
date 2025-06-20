using System.Text.Json;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Apps.UiInterop;

internal abstract class IpcResponsePromise
{
    /// <summary>
    /// Set the received response (ie. receive the response), triggering any waiting code to continue.
    /// </summary>
    public abstract void SetResponse(JsonElement response);
}

/// <summary>
/// Represents a promise to respond with data (a response).
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
internal class IpcResponsePromise<TResponse> : IpcResponsePromise
{
    private readonly TaskCompletionSource<TResponse?> _taskCompletionSource;

    public IpcResponsePromise()
    {
        _taskCompletionSource = new TaskCompletionSource<TResponse?>();
        Task = _taskCompletionSource.Task;
    }

    /// <summary>
    /// A task that returns once the response is received.
    /// </summary>
    public Task<TResponse?> Task { get; }

    /// <summary>
    /// Set the received response. Triggers the <see cref="Task"/> property to complete.
    /// </summary>
    public override void SetResponse(JsonElement responseJson)
    {
        var response = responseJson.Deserialize<TResponse>(JsonSerializer.DefaultOptions);
        _taskCompletionSource.SetResult(response);
    }
}
