using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NetPad.CodeAnalysis;
using NetPad.Exceptions;
using NetPad.Sessions;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Controllers;

[ApiController]
[Route("code")]
public class CodeController : ControllerBase
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = JsonSerializer.Configure(new JsonSerializerOptions
    {
        MaxDepth = 100
    });

    [HttpGet("{scriptId:guid}/syntax-tree")]
    public ActionResult<SyntaxNodeOrTokenSlim?> GetSyntaxTree(
        Guid scriptId,
        [FromServices] ISession session,
        [FromServices] ICodeAnalysisService codeAnalysisService)
    {
        var env = session.Get(scriptId);

        if (env == null)
        {
            throw new ScriptNotFoundException(scriptId);
        }

        var script = env.Script;

        if (string.IsNullOrWhiteSpace(script.Code))
        {
            return Ok(null);
        }

        var tree = codeAnalysisService.GetSyntaxTreeSlim(
            script.Code,
            script.Config.TargetFrameworkVersion,
            script.Config.OptimizationLevel,
            HttpContext.RequestAborted
        );

        return new JsonResult(tree, _jsonSerializerOptions);
    }
}
