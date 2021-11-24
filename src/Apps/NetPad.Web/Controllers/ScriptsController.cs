using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Mvc;
using NetPad.Common;
using NetPad.Exceptions;
using NetPad.Scripts;
using NetPad.Runtimes;
using NetPad.Services;
using NetPad.Sessions;
using NetPad.Utils;

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

        [HttpPatch("{id:guid}/save")]
        public async Task Save(Guid id)
        {
            var scriptEnvironment = GetScriptEnvironment(id);
            var script = scriptEnvironment.Script;

            await SaveAsync(script, _scriptRepository, _uiScriptService, _settings);
        }

        [HttpPatch("{id:guid}/run")]
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

        [HttpPatch("{id:guid}/open-config")]
        public async Task OpenConfig(Guid id)
        {
            var scriptEnvironment = GetScriptEnvironment(id);
            var script = scriptEnvironment.Script;

            var display = await Electron.Screen.GetPrimaryDisplayAsync();

            var window = await ElectronUtil.CreateWindowAsync("script-config", new BrowserWindowOptions()
            {
                Title = script.Name,
                Height = display.Bounds.Height * 2 / 4,
                Width = display.Bounds.Width * 2 / 4,
                MinHeight = 200,
                MinWidth = 200,
                Center = true,
                AutoHideMenuBar = true,
            }, ("script-id", script.Id));

            window.SetParentWindow(ElectronUtil.MainWindow);
        }

        [HttpPut("{id:guid}/config")]
        public IActionResult SetConfig(Guid id, [FromBody] ScriptConfig config)
        {
            var script = GetScriptEnvironment(id).Script;
            script.Config.SetKind(config.Kind);
            script.Config.SetNamespaces(config.Namespaces);

            return NoContent();
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
