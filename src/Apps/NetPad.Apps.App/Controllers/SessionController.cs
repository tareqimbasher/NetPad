using System;
using System.Collections.Generic;
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
    [Route("session")]
    public class SessionController : Controller
    {
        private readonly ISession _session;
        private readonly IScriptRepository _scriptRepository;
        private readonly IAutoSaveScriptRepository _autoSaveScriptRepository;
        private readonly IUiDialogService _uiDialogService;

        public SessionController(
            ISession session,
            IScriptRepository scriptRepository,
            IAutoSaveScriptRepository autoSaveScriptRepository,
            IUiDialogService uiDialogService)
        {
            _session = session;
            _scriptRepository = scriptRepository;
            _autoSaveScriptRepository = autoSaveScriptRepository;
            _uiDialogService = uiDialogService;
        }

        [HttpGet("environments/{scriptId:guid}")]
        public ScriptEnvironment GetEnvironment(Guid scriptId)
        {
            return _session.Get(scriptId) ?? throw new EnvironmentNotFoundException(scriptId);
        }

        [HttpGet("environments")]
        public IEnumerable<ScriptEnvironment> GetEnvironments()
        {
            return _session.Environments;
        }

        [HttpPatch("open/path")]
        public async Task OpenByPath([FromBody] string scriptPath)
        {
            var script = await _scriptRepository.GetAsync(scriptPath);
            await _session.OpenAsync(script);
        }

        [HttpPatch("{scriptId:guid}/close")]
        public async Task Close(Guid scriptId)
        {
            var scriptEnvironment = GetScriptEnvironment(scriptId);
            var script = scriptEnvironment.Script;

            if (script.IsDirty)
            {
                var response = await _uiDialogService.AskUserIfTheyWantToSave(script);
                if (response == YesNoCancel.Cancel)
                    return;

                if (response == YesNoCancel.Yes)
                {
                    bool saved = await ScriptsController.SaveAsync(script, _scriptRepository, _autoSaveScriptRepository, _uiDialogService);
                    if (!saved)
                        return;
                }
            }

            await _session.CloseAsync(scriptId);
            await _autoSaveScriptRepository.DeleteAsync(script);
        }

        [HttpGet("active")]
        public Guid? GetActive()
        {
            return _session.Active?.Script.Id;
        }

        [HttpPatch("{scriptId:guid}/activate")]
        public async Task Activate(Guid scriptId)
        {
            await _session.ActivateAsync(scriptId);
        }

        [HttpPatch("activate-last-active")]
        public async Task ActivateLastActive()
        {
            await _session.ActivateLastActiveScriptAsync();
        }

        private ScriptEnvironment GetScriptEnvironment(Guid id)
        {
            return _session.Get(id) ?? throw new ScriptNotFoundException(id);
        }
    }
}
