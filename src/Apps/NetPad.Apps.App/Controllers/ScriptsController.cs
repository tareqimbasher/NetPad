using System.Collections.Generic;
using System.Linq;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Dtos;
using NetPad.Exceptions;
using NetPad.ExecutionModel;
using NetPad.Scripts;
using NetPad.Services;
using NetPad.Sessions;

namespace NetPad.Controllers;

[ApiController]
[Route("scripts")]
public class ScriptsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ScriptSummary>> GetScripts()
    {
        return await mediator.Send(new GetAllScriptsQuery());
    }

    [HttpGet("{id:guid}/code")]
    public async Task<string> GetCode(
        Guid id,
        [FromServices] ISession session,
        [FromServices] IScriptRepository scriptRepository)
    {
        // Try open environment first, fall back to repository
        var environment = session.Get(id);
        if (environment != null)
        {
            return environment.Script.Code;
        }

        var script = await scriptRepository.GetAsync(id);
        if (script == null)
        {
            throw new ScriptNotFoundException(id);
        }

        return script.Code;
    }

    [HttpGet("find")]
    public async Task<IEnumerable<ScriptSummary>> FindScripts([FromQuery] string name)
    {
        var allScripts = await mediator.Send(new GetAllScriptsQuery());
        return allScripts.Where(s => s.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    [HttpPatch("create")]
    public async Task<Script> Create(
        [FromBody] CreateScriptDto dto,
        [FromServices] IDataConnectionRepository dataConnectionRepository)
    {
        var script = await mediator.Send(new CreateScriptCommand(dto.Name));

        bool hasSeedCode = !string.IsNullOrWhiteSpace(dto.Code);
        if (hasSeedCode)
        {
            await mediator.Send(new UpdateScriptCodeCommand(script, dto.Code));
        }

        if (dto.DataConnectionId != null)
        {
            var dataConnection = await dataConnectionRepository.GetAsync(dto.DataConnectionId.Value);
            await mediator.Send(new SetScriptDataConnectionCommand(script, dataConnection));
        }

        await mediator.Send(new OpenScriptCommand(script));

        if (hasSeedCode && dto.RunImmediately)
        {
            await mediator.Send(new RunScriptCommand(script.Id, new RunOptions()));
        }

        return script;
    }

    [HttpPatch("{id:guid}/rename")]
    public async Task Rename(Guid id, [FromBody] string newName)
    {
        var environment = await GetScriptEnvironmentAsync(id);
        await mediator.Send(new RenameScriptCommand(environment.Script, newName));
    }

    [HttpPatch("{id:guid}/duplicate")]
    public async Task Duplicate(Guid id)
    {
        var environment = await GetScriptEnvironmentAsync(id);
        var script = await mediator.Send(new DuplicateScriptCommand(environment.Script));
        await mediator.Send(new OpenScriptCommand(script));
    }

    [HttpPatch("{id:guid}/save")]
    public async Task<bool> Save(Guid id, [FromServices] ScriptService scriptService)
    {
        return await scriptService.SaveScriptAsync(id);
    }

    [HttpPatch("{id:guid}/run")]
    public async Task Run(
        Guid id,
        [FromBody] RunOptions options,
        [FromQuery] bool captureOutput,
        [FromServices] ScriptOutputCaptureService captureService)
    {
        if (captureOutput)
        {
            captureService.StartCapture(id);
        }

        await mediator.Send(new RunScriptCommand(id, options));
    }

    [HttpGet("{id:guid}/run-output")]
    public async Task<HeadlessRunResult> GetRunOutput(
        Guid id,
        [FromQuery] bool wait,
        [FromQuery] int? timeoutMs,
        [FromServices] ScriptOutputCaptureService captureService,
        CancellationToken cancellationToken)
    {
        return await captureService.GetCapturedOutputAsync(id, wait, timeoutMs, cancellationToken);
    }

    [HttpPatch("{id:guid}/stop")]
    public async Task Stop(Guid id, bool stopRunner = false)
    {
        await mediator.Send(new StopScriptCommand(id, stopRunner));
    }

    [HttpPatch("stop-all")]
    public async Task StopAll(bool force)
    {
        await mediator.Send(new StopAllScriptsCommand(force));
    }

    [HttpPut("{id:guid}/code")]
    public async Task UpdateCode(Guid id, [FromBody] string code, [FromQuery] bool externallyInitiated = false)
    {
        var environment = await GetScriptEnvironmentAsync(id);
        await mediator.Send(new UpdateScriptCodeCommand(environment.Script, code, externallyInitiated));
    }

    [HttpPatch("{id:guid}/open-config")]
    public async Task OpenConfigWindow(
        [FromServices] IUiWindowService uiWindowService,
        Guid id,
        [FromQuery] string? tab = null)
    {
        var environment = await GetScriptEnvironmentAsync(id);
        var script = environment.Script;
        await uiWindowService.OpenScriptConfigWindowAsync(script, tab);
    }

    [HttpPut("{id:guid}/namespaces")]
    public async Task<IActionResult> SetScriptNamespaces(Guid id, [FromBody] string[] namespaces)
    {
        var environment = await GetScriptEnvironmentAsync(id);

        await mediator.Send(new UpdateScriptNamespacesCommand(environment.Script, namespaces));

        return NoContent();
    }

    [HttpPut("{id:guid}/references")]
    public async Task<IActionResult> SetReferences(Guid id, [FromBody] Reference[] newReferences)
    {
        var environment = await GetScriptEnvironmentAsync(id);

        await mediator.Send(new UpdateScriptReferencesCommand(environment.Script, newReferences));

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
    public async Task<IActionResult> SetTargetFrameworkVersion(
        Guid id,
        [FromBody] DotNetFrameworkVersion targetFrameworkVersion)
    {
        var environment = await GetScriptEnvironmentAsync(id);

        await mediator.Send(new UpdateScriptTargetFrameworkCommand(environment.Script, targetFrameworkVersion));

        return NoContent();
    }

    [HttpPut("{id:guid}/optimization-level")]
    public async Task<IActionResult> SetOptimizationLevel(Guid id, [FromBody] OptimizationLevel optimizationLevel)
    {
        var environment = await GetScriptEnvironmentAsync(id);

        await mediator.Send(new UpdateScriptOptimizationLevelCommand(environment.Script, optimizationLevel));

        return NoContent();
    }

    [HttpPut("{id:guid}/use-asp-net")]
    public async Task<IActionResult> SetUseAspNet(Guid id, [FromBody] bool useAspNet)
    {
        var environment = await GetScriptEnvironmentAsync(id);

        await mediator.Send(new UpdateScriptUseAspNetCommand(environment.Script, useAspNet));

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

        await mediator.Send(new SetScriptDataConnectionCommand(environment.Script, dataConnection));
        return NoContent();
    }

    [HttpPatch("{id:guid}/mem-cache/dump")]
    public async Task DumpMemCacheItem(Guid id, [FromQuery] string key)
    {
        var environment = await GetScriptEnvironmentAsync(id);
        environment.DumpMemCacheItem(key);
    }

    [HttpDelete("{id:guid}/mem-cache")]
    public async Task DeleteMemCacheItem(Guid id, [FromQuery] string key)
    {
        var environment = await GetScriptEnvironmentAsync(id);
        environment.DeleteMemCacheItem(key);
    }

    [HttpDelete("{id:guid}/mem-cache/all")]
    public async Task ClearMemCacheItems(Guid id)
    {
        var environment = await GetScriptEnvironmentAsync(id);
        environment.ClearMemCacheItems();
    }

    private async Task<ScriptEnvironment> GetScriptEnvironmentAsync(Guid id)
    {
        return await mediator.Send(new GetOpenedScriptEnvironmentQuery(id, true))
               ?? throw new ScriptNotFoundException(id);
    }
}
