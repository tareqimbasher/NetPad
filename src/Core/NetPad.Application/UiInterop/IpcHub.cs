using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace NetPad.UiInterop;

/// <summary>
/// SignalR Hub. This includes methods that clients can invoke.
/// </summary>
public class IpcHub : Hub
{
    public void Respond(Guid messageId, JsonElement response)
    {
        IpcResponseQueue.ResponseReceived(messageId, response);
    }
}
