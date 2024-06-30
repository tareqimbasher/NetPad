using Microsoft.AspNetCore.Mvc;
using NetPad.Apps.UiInterop;

namespace NetPad.Controllers;

[ApiController]
[Route("window")]
public class WindowController(IUiWindowService uiWindowService) : ControllerBase
{
    [HttpPatch("open-output-window")]
    public async Task OpenOutputWindow()
    {
        await uiWindowService.OpenOutputWindowAsync();
    }

    [HttpPatch("open-code-window")]
    public async Task OpenCodeWindow()
    {
        await uiWindowService.OpenCodeWindowAsync();
    }
}
