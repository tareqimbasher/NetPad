using Microsoft.AspNetCore.Mvc;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueriesController : Controller
    {
        [HttpGet]
        public string[] Index()
        {
            return new[] { "Query 1", "Query 2" };
        }
    }
}