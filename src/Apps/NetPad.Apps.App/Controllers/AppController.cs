using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using NetPad.Configuration;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("app")]
    public class AppController : Controller
    {
        [HttpPatch("open-folder-containing-script")]
        public IActionResult OpenFolderContainingScript([FromQuery] string? scriptPath)
        {
            if (scriptPath == null)
                return BadRequest();

            var dirPath = Path.GetDirectoryName(scriptPath);
            if (!Directory.Exists(dirPath))
                return BadRequest($"Directory does not exist at: {dirPath}");

            Process.Start(new ProcessStartInfo
            {
                FileName = dirPath,
                UseShellExecute = true
            });
            return Ok();
        }

        [HttpPatch("open-scripts-folder")]
        public IActionResult OpenScriptsFolder([FromQuery] string? path, [FromServices] Settings settings)
        {
            string sanitized;

            if (string.IsNullOrWhiteSpace(path))
                sanitized = settings.ScriptsDirectoryPath;
            else
                sanitized = Path.Combine(settings.ScriptsDirectoryPath, path.Trim('.', '/', '\\'));

            if (!Directory.Exists(sanitized))
                return BadRequest($"Directory does not exist at: {path}");

            Process.Start(new ProcessStartInfo
            {
                FileName = sanitized,
                UseShellExecute = true
            });
            return Ok();
        }

        [HttpPatch("open-package-cache-folder")]
        public IActionResult OpenPackageCacheFolder([FromServices] Settings settings)
        {
            var path = settings.PackageCacheDirectoryPath;
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                throw new Exception($"Package cache folder does not exist at: '{settings.PackageCacheDirectoryPath}'");

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
            return Ok();
        }
    }
}
