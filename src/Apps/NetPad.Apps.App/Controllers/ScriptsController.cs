using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.Configuration;
using NetPad.Exceptions;
using NetPad.Scripts;
using NetPad.Sessions;
using NetPad.UiInterop;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("scripts")]
    public class ScriptsController : Controller
    {
        private readonly IScriptRepository _scriptRepository;
        private readonly IAutoSaveScriptRepository _autoSaveScriptRepository;
        private readonly ISession _session;
        private readonly IScriptNameGenerator _scriptNameGenerator;
        private readonly IUiDialogService _uiDialogService;

        public ScriptsController(
            IScriptRepository scriptRepository,
            IAutoSaveScriptRepository autoSaveScriptRepository,
            ISession session,
            IScriptNameGenerator scriptNameGenerator,
            IUiDialogService uiDialogService)
        {
            _scriptRepository = scriptRepository;
            _autoSaveScriptRepository = autoSaveScriptRepository;
            _session = session;
            _scriptNameGenerator = scriptNameGenerator;
            _uiDialogService = uiDialogService;
        }

        [HttpGet]
        public async Task<IEnumerable<ScriptSummary>> GetScripts()
        {
            return (await _scriptRepository.GetAllAsync()).OrderBy(s => s.Path);
        }

        [HttpPatch("create")]
        public async Task Create()
        {
            var name = _scriptNameGenerator.Generate();
            var script = await _scriptRepository.CreateAsync(name);
            await _session.OpenAsync(script);
        }

        [HttpPatch("{id:guid}/save")]
        public async Task Save(Guid id)
        {
            var scriptEnvironment = GetScriptEnvironment(id);
            var script = scriptEnvironment.Script;

            await SaveAsync(script, _scriptRepository, _autoSaveScriptRepository, _uiDialogService);
        }

        [HttpPatch("{id:guid}/run")]
        public async Task Run(Guid id)
        {
            var scriptEnvironment = GetScriptEnvironment(id);
            await scriptEnvironment.RunAsync();
        }

        [HttpPut("{id:guid}/code")]
        public void UpdateCode(Guid id, [FromBody] string code)
        {
            var scriptEnvironment = GetScriptEnvironment(id);
            var script = scriptEnvironment.Script;

            script.UpdateCode(code);
        }

        [HttpPatch("{id:guid}/open-config")]
        public async Task OpenConfigWindow(Guid id, [FromServices] IUiWindowService uiWindowService)
        {
            var scriptEnvironment = GetScriptEnvironment(id);
            var script = scriptEnvironment.Script;
            await uiWindowService.OpenScriptConfigWindowAsync(script);
        }

        [HttpPut("{id:guid}/namespaces")]
        public IActionResult SetScriptNamespaces(Guid id, [FromBody] IEnumerable<string> namespaces)
        {
            var script = GetScriptEnvironment(id).Script;
            script.Config.SetNamespaces(namespaces);

            return NoContent();
        }

        [HttpPut("{id:guid}/references")]
        public IActionResult SetReferences(Guid id, [FromBody] IEnumerable<Reference> references)
        {
            var script = GetScriptEnvironment(id).Script;
            script.Config.SetReferences(references);

            return NoContent();
        }

        [HttpPut("{id:guid}/kind")]
        public IActionResult SetScriptKind(Guid id, [FromBody] ScriptKind scriptKind)
        {
            var script = GetScriptEnvironment(id).Script;
            script.Config.SetKind(scriptKind);
            return NoContent();
        }

        // TODO needs better structure. maybe move to a mediator command (along with other calls)
        public static async Task<bool> SaveAsync(
            Script script,
            IScriptRepository scriptRepository,
            IAutoSaveScriptRepository autoSaveScriptRepository,
            IUiDialogService uiDialogService)
        {
            if (script.IsNew)
            {
                var path = await uiDialogService.AskUserForSaveLocation(script);
                if (string.IsNullOrWhiteSpace(path))
                    return false;

                script.SetPath(path);
            }

            await scriptRepository.SaveAsync(script);

            await autoSaveScriptRepository.DeleteAsync(script);

            return true;
        }

        private ScriptEnvironment GetScriptEnvironment(Guid id)
        {
            return _session.Get(id) ?? throw new ScriptNotFoundException(id);
        }
    }
}
