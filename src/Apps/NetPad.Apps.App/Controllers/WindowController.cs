using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.UiInterop;

namespace NetPad.Controllers;

[ApiController]
[Route("window")]
public class WindowController : Controller
{
    private readonly IUiWindowService _uiWindowService;

    public WindowController(IUiWindowService uiWindowService)
    {
        _uiWindowService = uiWindowService;
    }

    [HttpGet("state")]
    public async Task<WindowState?> GetState()
    {
        return await _uiWindowService.GetWindowStateAsync();
    }

    [HttpPatch("maximize")]
    public async Task Maximize()
    {
        await _uiWindowService.MaximizeMainWindowAsync();
    }

    [HttpPatch("minimize")]
    public async Task Minimize()
    {
        await _uiWindowService.MinimizeMainWindowAsync();
    }

    [HttpPatch("toggle-full-screen")]
    public async Task ToggleFullScreen()
    {
        await _uiWindowService.ToggleFullScreenAsync();
    }

    [HttpPatch("always-on-top/toggle")]
    public async Task ToggleAlwaysOnTop()
    {
        await _uiWindowService.ToggleAlwaysOnTopMainWindowAsync();
    }

    [HttpPatch("/{windowId:guid}/open-developer-tools")]
    public async Task OpenDeveloperTools(Guid windowId)
    {
        await _uiWindowService.OpenDeveloperToolsAsync(windowId);
    }

    [HttpPatch("open-output-window")]
    public async Task OpenOutputWindow()
    {
        await _uiWindowService.OpenOutputWindowAsync();
    }
}
