using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Mvc;
using NetPad.Exceptions;
using NetPad.Queries;
using NetPad.Runtimes;
using NetPad.Runtimes.Compilation;
using NetPad.Sessions;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("queries")]
    public class QueriesController : Controller
    {
        private readonly IQueryRepository _queryRepository;
        private readonly ISession _session;
        private readonly Settings _settings;

        public QueriesController(IQueryRepository queryRepository, ISession session, Settings settings)
        {
            _queryRepository = queryRepository;
            _session = session;
            _settings = settings;
        }

        [HttpGet("empty")]
        public Task<Query> Empty()
        {
            return Task.FromResult(new Query("Empty"));
        }

        [HttpGet]
        public async Task<IEnumerable<QuerySummary>> GetQueries()
        {
            return await _queryRepository.GetAllAsync();
        }

        [HttpPatch("create")]
        public async Task Create()
        {
            await _queryRepository.CreateAsync();
        }

        [HttpPatch("open")]
        public async Task Open([FromQuery] string filePath)
        {
            await _queryRepository.OpenAsync(filePath);
        }

        [HttpPatch("save/{id:guid}")]
        public async Task<bool> Save(Guid id)
        {
            var query = _session.Get(id);
            if (query == null)
                throw new Exception("Query not found");

            if (query.IsNew)
            {
                var path = await Electron.Dialog.ShowSaveDialogAsync(Electron.WindowManager.BrowserWindows.First(), new SaveDialogOptions()
                {
                    Title = "Save Query",
                    Message = "Where do you want to save this query?",
                    NameFieldLabel = query.Name,
                    Filters = new[] { new FileFilter { Name = "NetPad Queries", Extensions = new[] { "netpad" } } },
                    DefaultPath = _settings.QueriesDirectoryPath,
                });

                if (string.IsNullOrWhiteSpace(path))
                    return false;

                query.SetFilePath(path);
            }

            await _queryRepository.SaveAsync(query);

            return true;
        }

        [HttpPatch("close/{id:guid}")]
        public async Task Close(Guid id)
        {
            var query = _session.Get(id);
            if (query == null)
                throw new Exception("Query not found");

            if (query.IsDirty)
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
                    if (!await Save(query.Id))
                        return;
                }
                else if (result?.Response == 2)
                    return;
            }

            await _queryRepository.CloseAsync(id);
        }

        [HttpPatch("run/{id:guid}")]
        public async Task<string> Run(Guid id, [FromServices] IQueryRuntime queryRuntime)
        {
            var query = _session.Get(id);
            if (query == null)
                return "Query not found";

            var results = string.Empty;

            await queryRuntime.InitializeAsync(query);

            try
            {
                await queryRuntime.RunAsync(null, new TestQueryRuntimeOutputWriter(output => { results += output; }));
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

        [HttpPut("query/{id:guid}/code")]
        public void UpdateCode(Guid id, [FromBody] string code)
        {
            var query = _session.Get(id);
            if (query == null)
                return;

            query.UpdateCode(code);
        }
    }

    public class TestQueryRuntimeOutputWriter : IQueryRuntimeOutputWriter
    {
        private readonly Action<object?> _action;

        public TestQueryRuntimeOutputWriter(Action<object?> action)
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
