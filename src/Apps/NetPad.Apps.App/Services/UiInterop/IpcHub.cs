using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;

namespace NetPad.Services.UiInterop;

/// <summary>
/// SignalR Hub. This includes methods that clients can invoke.
/// </summary>
public class IpcHub(IMediator mediator) : Hub
{
    public void Respond(Guid messageId, JsonElement response)
    {
        IpcResponseQueue.ResponseReceived(messageId, response);
    }

    public async Task Activate(Guid scriptId)
    {
        await mediator.Send(new ActivateScriptCommand(scriptId));
    }

    public async Task ActivateLastActive()
    {
        await mediator.Send(new ActivateLastActiveScriptCommand());
    }
}
