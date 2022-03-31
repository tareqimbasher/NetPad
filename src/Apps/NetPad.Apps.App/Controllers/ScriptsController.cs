using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetPad.CQs;
using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.Controllers
{
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
        public async Task Create()
        {
            var script = await _mediator.Send(new CreateScriptCommand());
            await _mediator.Send(new OpenScriptCommand(script));
        }

        [HttpPatch("{id:guid}/save")]
        public async Task Save(Guid id)
        {
            var environment = await GetScriptEnvironmentAsync(id);
            await _mediator.Send(new SaveScriptCommand(environment.Script));
        }

        [HttpPatch("{id:guid}/run")]
        public async Task Run(Guid id)
        {
            await _mediator.Send(new RunScriptCommand(id));
        }

        [HttpPut("{id:guid}/code")]
        public async Task UpdateCode(Guid id, [FromBody] string code)
        {
            var environment = await GetScriptEnvironmentAsync(id);
            await _mediator.Send(new UpdateScriptCodeCommand(environment.Script, code));
        }

        [HttpPatch("{id:guid}/open-config")]
        public async Task OpenConfigWindow(Guid id, [FromServices] IUiWindowService uiWindowService)
        {
            var environment = await GetScriptEnvironmentAsync(id);
            var script = environment.Script;
            await uiWindowService.OpenScriptConfigWindowAsync(script);
        }

        [HttpPut("{id:guid}/namespaces")]
        public async Task<IActionResult> SetScriptNamespaces(Guid id, [FromBody] IEnumerable<string> namespaces)
        {
            var environment = await GetScriptEnvironmentAsync(id);
            environment.Script.Config.SetNamespaces(namespaces);

            return NoContent();
        }

        [HttpPut("{id:guid}/references")]
        public async Task<IActionResult> SetReferences(Guid id, [FromBody] IEnumerable<Reference> references)
        {
            var environment = await GetScriptEnvironmentAsync(id);
            environment.Script.Config.SetReferences(references);

            return NoContent();
        }

        [HttpPut("{id:guid}/kind")]
        public async Task<IActionResult> SetScriptKind(Guid id, [FromBody] ScriptKind scriptKind)
        {
            var environment = await GetScriptEnvironmentAsync(id);
            environment.Script.Config.SetKind(scriptKind);
            return NoContent();
        }

        private async Task<ScriptEnvironment> GetScriptEnvironmentAsync(Guid id)
        {
            var environment = await _mediator.Send(new GetOpenedScriptEnviornmentQuery(id, true));
            return environment!;
        }
    }
}
