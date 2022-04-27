using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.Services;
using OmniSharp.Models.v1.Completion;

namespace NetPad.Controllers;

[ApiController]
[Route("omnisharp")]
public class OmniSharpController : Controller
{
    private readonly OmniSharpServerCatalog _omniSharpServerCatalog;

    public OmniSharpController(OmniSharpServerCatalog omniSharpServerCatalog)
    {
        _omniSharpServerCatalog = omniSharpServerCatalog;
    }

    [HttpPost("{scriptId:guid}/completion")]
    public async Task<CompletionResponse?> GetCompletion(Guid scriptId, [FromBody] CompletionRequest request)
    {
        var server = GetOmniSharpServer(scriptId);
        if (server == null)
        {
            return null;
        }

        return await server.Send<CompletionResponse>(new CompletionRequest
        {
            Line = server.UserCodeStartsOnLine + request.Line,
            Column = request.Column,
            FileName = server.ProgramFilePath,
            CompletionTrigger = request.CompletionTrigger,
            TriggerCharacter = request.TriggerCharacter
        });
    }

    private AppOmniSharpServer? GetOmniSharpServer(Guid scriptId)
    {
        return _omniSharpServerCatalog.GetOmniSharpServer(scriptId);
    }
}
