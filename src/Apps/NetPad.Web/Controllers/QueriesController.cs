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

        [HttpGet]
        public async Task<string[]> GetQueries()
        {
            var directory = await _queryManager.GetQueriesDirectoryAsync();
            return directory.GetFiles("*.netpad").Select(f => f.Name).ToArray();
        }

        [HttpGet("open")]
        public async Task<Query> OpenQuery([FromQuery] string filePath)
        {
            var query = await _queryManager.OpenQueryAsync(filePath);
            return query;
        }
    }
}
