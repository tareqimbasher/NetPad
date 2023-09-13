using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetPad.CQs;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Dtos;
using NetPad.Runtimes;
using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.Controllers;

[ApiController]
[Route("scripts")]
public class ScriptsController : Controller
{
    private readonly IMediator _mediator;

    public ScriptsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IEnumerable<ScriptSummary>> GetScripts()
    {
        return await _mediator.Send(new GetAllScriptsQuery());
    }

    [HttpPatch("create")]
    public async Task Create([FromBody] CreateScriptDto dto, [FromServices] IDataConnectionRepository dataConnectionRepository)
    {
        var script = await _mediator.Send(new CreateScriptCommand());

        bool hasSeedCode = !string.IsNullOrWhiteSpace(dto.Code);
        if (hasSeedCode)
        {
            await _mediator.Send(new UpdateScriptCodeCommand(script, dto.Code));
        }

        if (dto.DataConnectionId != null)
        {
            var dataConnection = await dataConnectionRepository.GetAsync(dto.DataConnectionId.Value);
            await _mediator.Send(new SetScriptDataConnectionCommand(script, dataConnection));
        }

        await _mediator.Send(new OpenScriptCommand(script));

        if (hasSeedCode && dto.RunImmediately)
        {
            await _mediator.Send(new RunScriptCommand(script.Id, new RunOptions()));
        }
    }

    [HttpPatch("{id:guid}/save")]
    public async Task Save(Guid id)
    {
        var environment = await GetScriptEnvironmentAsync(id);
        await _mediator.Send(new SaveScriptCommand(environment.Script));
    }

    [HttpPatch("{id:guid}/run")]
    public async Task Run(Guid id, [FromBody] RunOptionsDto dto)
    {
        await _mediator.Send(new RunScriptCommand(id, dto.ToRunOptions()));
    }

    [HttpPatch("{id:guid}/stop")]
    public async Task Stop(Guid id)
    {
        await _mediator.Send(new StopScriptCommand(id));
    }

    [HttpPut("{id:guid}/code")]
    public async Task UpdateCode(Guid id, [FromBody] string code)
    {
        var environment = await GetScriptEnvironmentAsync(id);
        await _mediator.Send(new UpdateScriptCodeCommand(environment.Script, code));
    }

    [HttpPatch("{id:guid}/open-config")]
    public async Task OpenConfigWindow([FromServices] IUiWindowService uiWindowService, Guid id, [FromQuery] string? tab = null)
    {
        var environment = await GetScriptEnvironmentAsync(id);
        var script = environment.Script;
        await uiWindowService.OpenScriptConfigWindowAsync(script, tab);
    }

    [HttpPut("{id:guid}/namespaces")]
    public async Task<IActionResult> SetScriptNamespaces(Guid id, [FromBody] IEnumerable<string> namespaces)
    {
        var environment = await GetScriptEnvironmentAsync(id);

        await _mediator.Send(new UpdateScriptNamespacesCommand(environment.Script, namespaces));

        return NoContent();
    }

    [HttpPut("{id:guid}/references")]
    public async Task<IActionResult> SetReferences(Guid id, [FromBody] IEnumerable<Reference> newReferences)
    {
        var environment = await GetScriptEnvironmentAsync(id);

        await _mediator.Send(new UpdateScriptReferencesCommand(environment.Script, newReferences));

        return NoContent();
    }

    [HttpPut("{id:guid}/kind")]
    public async Task<IActionResult> SetScriptKind(Guid id, [FromBody] ScriptKind scriptKind)
    {
        var environment = await GetScriptEnvironmentAsync(id);
        environment.Script.Config.SetKind(scriptKind);
        return NoContent();
    }

    [HttpPut("{id:guid}/target-framework-version")]
    public async Task<IActionResult> SetTargetFrameworkVersion(Guid id, [FromBody] DotNetFrameworkVersion targetFrameworkVersion)
    {
        var environment = await GetScriptEnvironmentAsync(id);

        await _mediator.Send(new UpdateScriptTargetFrameworkCommand(environment.Script, targetFrameworkVersion));

        return NoContent();
    }

    [HttpPut]
    [Route("{id:guid}/data-connection")]
    public async Task<IActionResult> SetDataConnection(
        Guid id,
        [FromQuery] Guid? dataConnectionId,
        [FromServices] IDataConnectionRepository dataConnectionRepository)
    {
        var environment = await GetScriptEnvironmentAsync(id);

        DataConnection? dataConnection = null;
        if (dataConnectionId != null)
        {
            dataConnection = await dataConnectionRepository.GetAsync(dataConnectionId.Value);
        }

        await _mediator.Send(new SetScriptDataConnectionCommand(environment.Script, dataConnection));
        return NoContent();
    }

    private async Task<ScriptEnvironment> GetScriptEnvironmentAsync(Guid id)
    {
        var environment = await _mediator.Send(new GetOpenedScriptEnvironmentQuery(id, true));
        return environment!;
    }
}
