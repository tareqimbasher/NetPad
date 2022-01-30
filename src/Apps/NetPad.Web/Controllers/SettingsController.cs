using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.Configuration;
using NetPad.Events;
using NetPad.UiInterop;

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
        public async Task<IActionResult> Update(
            [FromBody] Settings settings,
            [FromServices] ISettingsRepository settingsRepository,
            [FromServices] IIpcService ipcService)
        {
            _settings
                .SetTheme(settings.Theme)
                .SetScriptsDirectoryPath(settings.ScriptsDirectoryPath)
                .SetPackageCacheDirectoryPath(settings.PackageCacheDirectoryPath);

            await settingsRepository.SaveSettingsAsync(_settings);
            await ipcService.SendAsync(new SettingsUpdated(_settings));

            return NoContent();
        }
    }
}
