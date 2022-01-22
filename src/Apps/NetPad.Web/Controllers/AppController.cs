using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("app")]
    public class AppController : Controller
    {
        [HttpPatch("open-scripts-folder")]
        public IActionResult OpenScriptsFolder([FromQuery] string? path, [FromServices] Settings settings)
        {
            if (string.IsNullOrWhiteSpace(path))
                path = settings.ScriptsDirectoryPath;
            else
                path = Path.Combine(settings.ScriptsDirectoryPath, path.Trim('/'));

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
            return Ok();
        }
    }
}
