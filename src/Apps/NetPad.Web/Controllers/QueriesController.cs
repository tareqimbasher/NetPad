using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.Queries;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueriesController : Controller
    {
        private readonly IQueryManager _queryManager;

        public QueriesController(IQueryManager queryManager)
        {
            _queryManager = queryManager;
        }
        
        [HttpGet]
        public async Task<string[]> Index()
        {
            var directory = await _queryManager.GetQueriesDirectoryAsync();
            return directory.GetFiles("*.netpad").Select(f => f.Name).ToArray();
        }
    }
}