using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Mvc;
using NetPad.Exceptions;
using NetPad.Scripts;
using NetPad.Runtimes;
using NetPad.Runtimes.Compilation;
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

        public ScriptsController(IScriptRepository scriptRepository, ISession session, Settings settings)
        {
            _scriptRepository = scriptRepository;
            _session = session;
            _settings = settings;
        }

        [HttpGet("empty")]
        public Task<Script> Empty()
        {
            return Task.FromResult(new Script("Empty"));
        }

        [HttpGet]
        public async Task<IEnumerable<ScriptSummary>> GetScripts()
        {
            return await _scriptRepository.GetAllAsync();
        }

        [HttpPatch("create")]
        public async Task Create()
        {
            await _scriptRepository.CreateAsync();
        }

        [HttpPatch("open")]
        public async Task Open([FromQuery] string filePath)
        {
            await _scriptRepository.OpenAsync(filePath);
        }

        [HttpPatch("save/{id:guid}")]
        public async Task<bool> Save(Guid id)
        {
            var script = _session.Get(id);
            if (script == null)
                throw new Exception("Script not found");

            if (script.IsNew)
            {
                var path = await Electron.Dialog.ShowSaveDialogAsync(Electron.WindowManager.BrowserWindows.First(), new SaveDialogOptions()
                {
                    Title = "Save Script",
                    Message = "Where do you want to save this script?",
                    NameFieldLabel = script.Name,
                    Filters = new[] { new FileFilter { Name = "NetPad Script", Extensions = new[] { "netpad" } } },
                    DefaultPath = _settings.ScriptsDirectoryPath,
                });

                if (string.IsNullOrWhiteSpace(path))
                    return false;

                script.SetFilePath(path);
            }

            await _scriptRepository.SaveAsync(script);

            return true;
        }

        [HttpPatch("close/{id:guid}")]
        public async Task Close(Guid id)
        {
            var script = _session.Get(id);
            if (script == null)
                throw new Exception("Script not found");

            if (script.IsDirty)
            {
                var result = await Electron.Dialog.ShowMessageBoxAsync(Electron.WindowManager.BrowserWindows.First(),
                    new MessageBoxOptions("Do you want to save?")
                    {
                        Title = "Save?",
                        Buttons = new[] { "Yes", "No", "Cancel" },
                        Type = MessageBoxType.question
                    });

                if (result?.Response == 0)
                {
                    if (!await Save(script.Id))
                        return;
                }
                else if (result?.Response == 2)
                    return;
            }

            await _scriptRepository.CloseAsync(id);
        }

        [HttpPatch("run/{id:guid}")]
        public async Task<string> Run(Guid id, [FromServices] IScriptRuntime scriptRuntime)
        {
            var script = _session.Get(id);
            if (script == null)
                return "Script not found";

            var results = string.Empty;

            await scriptRuntime.InitializeAsync(script);

            try
            {
                await scriptRuntime.RunAsync(null, new TestScriptRuntimeOutputWriter(output => { results += output; }));
            }
            catch (CodeCompilationException ex)
            {
                results += ex.ErrorsAsString() + "\n";
            }
            catch (Exception ex)
            {
                results += ex + "\n";
            }

            return results;
        }

        [HttpPut("{id:guid}/code")]
        public void UpdateCode(Guid id, [FromBody] string code)
        {
            var script = _session.Get(id);
            if (script == null)
                return;

            script.UpdateCode(code);
        }
    }

    public class TestScriptRuntimeOutputWriter : IScriptRuntimeOutputWriter
    {
        private readonly Action<object?> _action;

        public TestScriptRuntimeOutputWriter(Action<object?> action)
        {
            _action = action;
        }

        public Task WriteAsync(object? output)
        {
            _action(output);
            return Task.CompletedTask;
        }
    }
}
