using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ElectronNET.API;
using Microsoft.AspNetCore.Mvc;
using NetPad.Common;
using NetPad.Exceptions;
using NetPad.Scripts;
using NetPad.Runtimes;
using NetPad.Services;
using NetPad.Sessions;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("scripts")]
    public class ScriptsController : Controller
    {
        private readonly IScriptRepository _scriptRepository;
        private readonly ISession _session;
        private readonly Settings _settings;
        private readonly IUiScriptService _uiScriptService;

        public ScriptsController(IScriptRepository scriptRepository, ISession session, Settings settings, IUiScriptService uiScriptService)
        {
            _scriptRepository = scriptRepository;
            _session = session;
            _settings = settings;
            _uiScriptService = uiScriptService;
        }

        [HttpGet]
        public async Task<IEnumerable<ScriptSummary>> GetScripts()
        {
            return await _scriptRepository.GetAllAsync();
        }

        [HttpPatch("create")]
        public async Task Create()
        {
            var name = await _session.GetNewScriptName();
            var script = await _scriptRepository.CreateAsync(name);
            await _session.OpenAsync(script);
        }

        [HttpPatch("save/{id:guid}")]
        public async Task Save(Guid id)
        {
            var scriptEnvironment = GetScriptEnvironment(id);
            var script = scriptEnvironment.Script;

            await SaveAsync(script, _scriptRepository, _uiScriptService, _settings);
        }

        [HttpPatch("run/{id:guid}")]
        public async Task Run(Guid id, [FromServices] IScriptRuntime scriptRuntime)
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

        public static async Task<bool> SaveAsync(Script script, IScriptRepository scriptRepository, IUiScriptService uiScriptService, Settings settings)
        {
            if (script.IsNew)
            {
                var path = await uiScriptService.AskUserForSaveLocation(script);
                if (string.IsNullOrWhiteSpace(path))
                    return false;

                script.SetPath(path.Replace(settings.ScriptsDirectoryPath, string.Empty));
            }

            await scriptRepository.SaveAsync(script);
            return true;
        }

        private ScriptEnvironment GetScriptEnvironment(Guid id)
        {
            return _session.Get(id) ?? throw new ScriptNotFoundException(id);
        }
    }
}
