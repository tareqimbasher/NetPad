using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;
using NetPad.Configuration;
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
public class ScriptsController(IMediator mediator, IScriptRepository scriptRepository, ISession session)
    : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ScriptSummary>> GetScripts()
    {
        return await mediator.Send(new GetAllScriptsQuery());
    }

    [HttpGet("info")]
    public async Task<IEnumerable<ScriptInfo>> GetScriptsInfo([FromQuery] string? name = null)
    {
        return await mediator.Send(new GetScriptsInfoQuery(name));
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
        var script = await GetScriptAsync(id);
        await mediator.Send(new RenameScriptCommand(script, newName));
    }

    [HttpPatch("{id:guid}/duplicate")]
    public async Task<Script> Duplicate(Guid id)
    {
        var script = await GetScriptAsync(id);
        var duplicate = await mediator.Send(new DuplicateScriptCommand(script));
        await mediator.Send(new OpenScriptCommand(duplicate));
        return duplicate;
    }

    [HttpPatch("{id:guid}/save")]
    public async Task<bool> Save(Guid id, [FromServices] ScriptService scriptService)
    {
        return await scriptService.SaveScriptAsync(id);
    }

    [HttpDelete("{id:guid}")]
    public async Task Delete(Guid id)
    {
        var script = await GetScriptAsync(id);
        await mediator.Send(new DeleteScriptCommand(script));
    }

    [HttpDelete("folder")]
    public async Task DeleteFolder(
        [FromQuery] string path,
        [FromServices] Settings settings,
        [FromServices] ScriptService scriptService,
        [FromServices] IAutoSaveScriptRepository autoSaveScriptRepository)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new Exception("A folder path must be provided.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(settings.ScriptsDirectoryPath, path.Trim('.', '/', '\\')));

        if (!fullPath.StartsWith(settings.ScriptsDirectoryPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fullPath, settings.ScriptsDirectoryPath, StringComparison.OrdinalIgnoreCase)
            || !Directory.Exists(fullPath))
        {
            throw new Exception($"Invalid or non-existent directory: {path}");
        }

        var openEnvironments = session.GetOpened()
            .Where(e => e.Script.Path?.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase) == true)
            .ToArray();

        if (openEnvironments.Length > 0)
        {
            foreach (var environment in openEnvironments)
            {
                await scriptService.CloseScriptAsync(environment.Script.Id, true);
            }
        }

        Directory.Delete(fullPath, recursive: true);
    }

    [HttpPatch("{id:guid}/run")]
    public async Task Run(Guid id, [FromBody] RunOptions options)
    {
        await mediator.Send(new RunScriptCommand(id, options));
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

    private async Task<Script> GetScriptAsync(Guid id)
    {
        var script = session.Get(id)?.Script;

        if (script == null)
        {
            script = await scriptRepository.GetAsync(id);
        }

        return script ?? throw new ScriptNotFoundException(id);
    }

    private async Task<ScriptEnvironment> GetScriptEnvironmentAsync(Guid id)
    {
        return await mediator.Send(new GetOpenedScriptEnvironmentQuery(id, true))
               ?? throw new ScriptNotFoundException(id);
    }
}
