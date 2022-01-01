using Microsoft.AspNetCore.Mvc;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("settings")]
    public class SettingsController : Controller
    {
        private readonly Settings _settings;

        public SettingsController(Settings settings)
        {
            _settings = settings;
        }

        [HttpGet]
        public Settings Get()
        {
            return _settings;
        }

        [HttpPut]
        public IActionResult Update([FromBody] Settings settings)
        {
            _settings
                .SetTheme(settings.Theme)
                .SetScriptsDirectoryPath(settings.ScriptsDirectoryPath);
            return NoContent();
        }
    }
}
