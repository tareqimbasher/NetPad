using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IQueryManager _queryManager;

        public QueriesController(IQueryManager queryManager)
        {
            _queryManager = queryManager;
        }

        [HttpGet("empty")]
        public Task<Query> Empty()
        {
            return Task.FromResult(new Query("Empty"));
        }

        [HttpGet]
        public async Task<string[]> GetQueries()
        {
            var directory = await _queryManager.GetQueriesDirectoryAsync();
            return directory.GetFiles("*.netpad", SearchOption.AllDirectories)
                .Select(f => f.FullName)
                .OrderBy(x => x)
                .ToArray();
        }

        [HttpPatch("create")]
        public async Task Create()
        {
            await _queryManager.CreateNewQueryAsync();
        }

        [HttpPatch("open")]
        public async Task Open([FromQuery] string filePath)
        {
            await _queryManager.OpenQueryAsync(filePath);
        }

        [HttpPatch("save/{id:guid}")]
        public async Task Save(Guid id, [FromServices] ISession session, [FromServices] Settings settings)
        {
            var query = session.Get(id);
            if (query == null)
                throw new Exception("Query not found");

            try
            {
                if (query.IsNew)
                {
                    var path = await Electron.Dialog.ShowSaveDialogAsync(Electron.WindowManager.BrowserWindows.First(), new SaveDialogOptions()
                    {
                        Title = "Save Query",
                        Message = "Where do you want to save this query?",
                        NameFieldLabel = query.Name,
                        Filters = new[] { new FileFilter { Name = "NetPad Queries", Extensions = new[] { "netpad" } } },
                        DefaultPath = settings.QueriesDirectoryPath,
                    });

                    if (string.IsNullOrWhiteSpace(path))
                        return;

                    if (!path.EndsWith(".netpad")) path += ".netpad";

                    query.SetFilePath(path);
                }

                Console.WriteLine("Saving: " + query.Code);
                await query.SaveAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPatch("close/{id:guid}")]
        public async Task Close(Guid id)
        {
            await _queryManager.CloseQueryAsync(id);
        }

        [HttpPatch("run/{id:guid}")]
        public async Task<string> Run(Guid id, [FromServices] ISession session)
        {
            var query = session.Get(id);
            if (query == null)
                return "Query not found";

            var results = string.Empty;

            var queryRuntime = new MainAppDomainQueryRuntime();
            await queryRuntime.InitializeAsync(query);

            try
            {
                await queryRuntime.RunAsync(null, new TestQueryRuntimeOutputReader(output =>
                {
                    results += output;
                }));
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
        public void UpdateCode(Guid id, [FromBody]string code, [FromServices] ISession session)
        {
            var query = session.Get(id);
            if (query == null)
                return;

            query.UpdateCode(code);
        }
    }

    public class TestQueryRuntimeOutputReader : IQueryRuntimeOutputReader
    {
        private readonly Action<object?> _action;

        public TestQueryRuntimeOutputReader(Action<object?> action)
        {
            _action = action;
        }

        public Task ReadAsync(object? output)
        {
            _action(output);
            return Task.CompletedTask;
        }
    }
}
