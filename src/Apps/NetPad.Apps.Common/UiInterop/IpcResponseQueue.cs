using System.Diagnostics.Contracts;
using System.Text.Json;
using NetPad.Apps.CQs;

namespace NetPad.Apps.UiInterop;

/// <summary>
/// A queue used to wait for clients to respond to requests.
/// </summary>
internal static class IpcResponseQueue
{
    private static readonly Dictionary<Guid, IpcResponsePromise> _promises = new();

    /// <summary>
    /// Enqueue a message we're waiting on for a response and returns a promise that will resolve only when a
    /// response is received.
    /// </summary>
    [Pure]
    public static IpcResponsePromise<TResponse> Enqueue<TResponse>(
        Command<TResponse> message,
        CancellationToken cancellationToken = default)
    {
        var promise = new IpcResponsePromise<TResponse>();
        _promises.Add(message.Id, promise);
        return promise;
    }

    /// <summary>
    /// Indicate a response was received.
    /// </summary>
    public static void ResponseReceived(Guid messageId, JsonElement response)
    {
        if (!_promises.Remove(messageId, out var promise))
            return;

        promise.SetResponse(response);
    }
}
