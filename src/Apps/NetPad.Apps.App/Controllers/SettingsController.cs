using System.IO;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;
using NetPad.Configuration;

namespace NetPad.Controllers;

[ApiController]
[Route("settings")]
public class SettingsController(Settings settings, IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Settings Get()
    {
        return settings;
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Settings update)
    {
        await mediator.Send(new UpdateSettingsCommand(update));
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

        ProcessUtil.OpenWithDefaultApp(containingDir);

        return Ok();
    }
}
