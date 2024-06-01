using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace NetPad.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[ApiController]
[Route("files")]
public class FilesController : ControllerBase
{
    [HttpGet("{path}")]
    public IActionResult StreamFile(string path)
    {
        path = Uri.UnescapeDataString(path);

        if (!System.IO.File.Exists(path))
            return NotFound();

        var result = File(System.IO.File.OpenRead(path), "application/octet-stream", Path.GetFileName(path));

        // To allow clients to request ranges of file (video/audio) to enable things like seeking a video
        result.EnableRangeProcessing = true;

        return result;
    }
}
