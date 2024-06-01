using System.Collections.Generic;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetPad.Apps.CQs;
using NetPad.Exceptions;
using NetPad.Scripts;
using NetPad.Services;

namespace NetPad.Controllers;

[ApiController]
[Route("session")]
public class SessionController(IMediator mediator) : ControllerBase
{
    [HttpGet("environments/{scriptId:guid}")]
    public async Task<ScriptEnvironment> GetEnvironment(Guid scriptId)
    {
        return await mediator.Send(new GetOpenedScriptEnvironmentQuery(scriptId))
               ?? throw new EnvironmentNotFoundException(scriptId);
    }

    [HttpGet("environments")]
    public async Task<IEnumerable<ScriptEnvironment>> GetEnvironments()
    {
        return await mediator.Send(new GetOpenedScriptEnvironmentsQuery());
    }

    [HttpPatch("open/path")]
    public async Task OpenByPath([FromBody] string scriptPath)
    {
        await mediator.Send(new OpenScriptCommand(scriptPath));
    }

    [HttpPatch("{scriptId:guid}/close")]
    public async Task Close(Guid scriptId, [FromServices] ScriptService scriptService)
    {
        await scriptService.CloseScriptAsync(scriptId);
    }

    [HttpGet("active")]
    public async Task<Guid?> GetActive()
    {
        var active = await mediator.Send(new GetActiveScriptEnvironmentQuery());
        return active?.Script.Id;
    }

    [HttpPatch("{scriptId:guid}/activate")]
    public async Task Activate(Guid scriptId)
    {
        await mediator.Send(new ActivateScriptCommand(scriptId));
    }

    [HttpPatch("activate-last-active")]
    public async Task ActivateLastActive()
    {
        await mediator.Send(new ActivateLastActiveScriptCommand());
    }
}
