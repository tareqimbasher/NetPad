using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.Queries;

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
            return directory.GetFiles("*.netpad").Select(f => f.Name).ToArray();
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

        [HttpPatch("close")]
        public async Task Close([FromQuery] Guid id)
        {
            await _queryManager.CloseQueryAsync(id);
        }
    }
}
