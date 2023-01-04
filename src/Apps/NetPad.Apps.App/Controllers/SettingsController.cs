using System.IO;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetPad.Configuration;
using NetPad.CQs;
using NetPad.UiInterop;
using NetPad.Utilities;

namespace NetPad.Controllers;

[ApiController]
[Route("settings")]
public class SettingsController : Controller
{
    private readonly Settings _settings;
    private readonly IMediator _mediator;

    public SettingsController(Settings settings, IMediator mediator)
    {
        _settings = settings;
        _mediator = mediator;
    }

    [HttpGet]
    public Settings Get()
    {
        return _settings;
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Settings settings)
    {
        await _mediator.Send(new UpdateSettingsCommand(settings));

        return NoContent();
    }

    [HttpPatch("open")]
    public async Task OpenSettingsWindow([FromServices] IUiWindowService uiWindowService, [FromQuery] string? tab = null)
    {
        await uiWindowService.OpenSettingsWindowAsync(tab);
    }

    [HttpPatch("show-settings-file")]
    public async Task<IActionResult> ShowSettingsFile([FromServices] ISettingsRepository settingsRepository)
    {
        var containingDir = (await settingsRepository.GetSettingsFileLocationAsync())
            .GetInfo()
            .DirectoryName;

        if (string.IsNullOrWhiteSpace(containingDir) || !Directory.Exists(containingDir))
            return Ok();

        ProcessUtils.OpenInDesktopExplorer(containingDir);

        return Ok();
    }
}
