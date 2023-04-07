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

    [HttpPatch("always-on-top/toggle")]
    public async Task ToggleAlwaysOnTop()
    {
        await _uiWindowService.ToggleAlwaysOnTopMainWindowAsync();
    }
}
