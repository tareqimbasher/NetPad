using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;

namespace NetPad.Services.UiInterop;

/// <summary>
/// A SignalR hub that contains methods that clients can invoke. This is the primary way for clients to send messages
/// to this application (host) over SignalR.
/// </summary>
public class IpcHub(IMediator mediator) : Hub
{
    /// <summary>
    /// Receives a response from a previously sent request.
    /// </summary>
    public void Respond(Guid messageId, JsonElement response)
    {
        IpcResponseQueue.ResponseReceived(messageId, response);
    }

    /// <summary>
    /// Sets the specified script as the active (focused) script.
    /// </summary>
    public async Task Activate(Guid scriptId)
    {
        await mediator.Send(new ActivateScriptCommand(scriptId));
    }

    /// <summary>
    /// Sets the last active script as the new active (focused) script.
    /// </summary>
    public async Task ActivateLastActive()
    {
        await mediator.Send(new ActivateLastActiveScriptCommand());
    }
}
