using System.Diagnostics;
using System.IO;
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
                .SetPackageCacheDirectoryPath(settings.PackageCacheDirectoryPath)
                .SetEditorBackgroundColor(settings.EditorBackgroundColor)
                .SetEditorOptions(settings.EditorOptions)
                .SetResultsOptions(settings.ResultsOptions)
            ;

            await settingsRepository.SaveSettingsAsync(_settings);
            await ipcService.SendAsync(new SettingsUpdated(_settings));

            return NoContent();
        }

        [HttpPatch("open")]
        public async Task OpenSettingsWindow([FromServices] IUiWindowService uiWindowService)
        {
            await uiWindowService.OpenSettingsWindowAsync();
        }

        [HttpPatch("show-settings-file")]
        public async Task<IActionResult> ShowSettingsFile([FromServices] ISettingsRepository settingsRepository)
        {
            var path = Path.GetDirectoryName(await settingsRepository.GetSettingsFileLocationAsync());
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return Ok();

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
            return Ok();
        }
    }
}
