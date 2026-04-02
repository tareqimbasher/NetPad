using Microsoft.AspNetCore.Mvc;
using NetPad.Dtos;
using NetPad.Services;

namespace NetPad.Controllers;

[ApiController]
[Route("api/headless")]
public class HeadlessController(HeadlessScriptExecutionService executionService) : ControllerBase
{
    [HttpPost("run")]
    public async Task<HeadlessRunResult> RunCode(
        [FromBody] HeadlessRunRequest request,
        CancellationToken cancellationToken)
    {
        return await executionService.RunCodeAsync(request, cancellationToken);
    }

    [HttpPost("run/{scriptId:guid}")]
    public async Task<HeadlessRunResult> RunScript(
        Guid scriptId,
        [FromQuery] int? timeoutMs,
        CancellationToken cancellationToken)
    {
        return await executionService.RunScriptAsync(scriptId, timeoutMs, cancellationToken);
    }
}
