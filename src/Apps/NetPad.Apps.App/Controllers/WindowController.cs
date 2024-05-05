using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.UiInterop;

namespace NetPad.Controllers;

[ApiController]
[Route("window")]
public class WindowController : ControllerBase
{
    private readonly IUiWindowService _uiWindowService;

    public WindowController(IUiWindowService uiWindowService)
    {
        _uiWindowService = uiWindowService;
    }

    [HttpPatch("open-output-window")]
    public async Task OpenOutputWindow()
    {
        await _uiWindowService.OpenOutputWindowAsync();
    }
}
