using System.Text.Json;

namespace NetPad.Apps.UiInterop;

/// <summary>
/// A queue used to wait for clients to respond to requests.
/// </summary>
internal static class IpcResponseQueue
{
    private static readonly Dictionary<Guid, IpcResponsePromise> _promises;

    static IpcResponseQueue()
    {
        _promises = new Dictionary<Guid, IpcResponsePromise>();
    }

    /// <summary>
    /// Enqueue a message we're waiting on for a response.
    /// </summary>
    public static void Enqueue(Guid messageId, IpcResponsePromise promise)
    {
        _promises.Add(messageId, promise);
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
