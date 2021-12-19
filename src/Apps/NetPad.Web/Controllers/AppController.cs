using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("app")]
    public class AppController : Controller
    {
        [HttpPatch("open-scripts-folder")]
        public IActionResult OpenScriptsFolder([FromServices] Settings settings)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = settings.ScriptsDirectoryPath,
                UseShellExecute = true
            });
            return Ok();
        }
    }
}
