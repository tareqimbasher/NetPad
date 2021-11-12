using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
            return directory.GetFiles("*.netpad").Select(f => f.FullName).ToArray();
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
        public void UpdateCode(Guid id, string code, [FromServices] ISession session)
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
