using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetPad.Plugins.OmniSharp.Features.CodeActions;
using NetPad.Plugins.OmniSharp.Features.CodeChecking;
using NetPad.Plugins.OmniSharp.Features.CodeFormatting;
using NetPad.Plugins.OmniSharp.Features.CodeStructure;
using NetPad.Plugins.OmniSharp.Features.Completion;
using NetPad.Plugins.OmniSharp.Features.FindImplementations;
using NetPad.Plugins.OmniSharp.Features.FindUsages;
using NetPad.Plugins.OmniSharp.Features.InlayHinting;
using NetPad.Plugins.OmniSharp.Features.QuickInfo;
using NetPad.Plugins.OmniSharp.Features.SemanticHighlighting;
using NetPad.Plugins.OmniSharp.Features.ServerManagement;
using NetPad.Plugins.OmniSharp.Features.SignatureHelp;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Plugins.OmniSharp;

[ApiController]
[Route("omnisharp/{scriptId:guid}")]
public class OmniSharpController : Controller
{
    private readonly IMediator _mediator;

    public OmniSharpController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPatch("restart-server")]
    public async Task<bool> RestartServer(Guid scriptId) => await _mediator.Send(new RestartOmniSharpServerCommand(scriptId));

    [HttpPost("completion")]
    public async Task<ActionResult<OmniSharpCompletionResponse?>> GetCompletion(Guid scriptId, [FromBody] OmniSharpCompletionRequest request)
    {
        var response = await _mediator.Send(new GetCompletionsQuery(scriptId, request));

        var serializationOptions = JsonSerializer.Configure(new JsonSerializerOptions()
        {
            IncludeFields = true // To serialize tuples correctly
        });

        return new JsonResult(response, serializationOptions);
    }

    [HttpPost("completion/resolve")]
    public async Task<ActionResult<OmniSharpCompletionResolveResponse?>> GetCompletionResolution(Guid scriptId, [FromBody] CompletionItem completionItem)
    {
        var response = await _mediator.Send(new ResolveCompletionQuery(scriptId, completionItem));

        var serializationOptions = JsonSerializer.Configure(new JsonSerializerOptions()
        {
            IncludeFields = true
        });

        return new JsonResult(response, serializationOptions);
    }

    [HttpPost("completion/after-insert")]
    public async Task<OmniSharpCompletionAfterInsertResponse?> GetCompletionAfterInsert(Guid scriptId, [FromBody] CompletionItem completionItem) =>
        await _mediator.Send(new GetCompletionAfterInsertQuery(scriptId, completionItem));

    [HttpPost("format-code")]
    public async Task<OmniSharpCodeFormatResponse?> FormatCode(Guid scriptId, [FromBody] OmniSharpCodeFormatRequest request) =>
        await _mediator.Send(new CodeFormatQuery(scriptId, request));

    [HttpPost("semantic-highlights")]
    public async Task<OmniSharpSemanticHighlightResponse?> GetSemanticHighlights(Guid scriptId, [FromBody] OmniSharpSemanticHighlightRequest request) =>
        await _mediator.Send(new GetSemanticHighlightsQuery(scriptId, request));

    [HttpPost("find-implementations")]
    public async Task<OmniSharpQuickFixResponse?> FindImplementations(Guid scriptId, [FromBody] OmniSharpFindImplementationsRequest request) =>
        await _mediator.Send(new FindImplementationsQuery(scriptId, request));

    [HttpPost("quick-info")]
    public async Task<OmniSharpQuickInfoResponse?> GetQuickInfo(Guid scriptId, [FromBody] OmniSharpQuickInfoRequest request) =>
        await _mediator.Send(new GetQuickInfoQuery(scriptId, request));

    [HttpPost("signature-help")]
    public async Task<OmniSharpSignatureHelpResponse?> GetSignatureHelp(Guid scriptId, [FromBody] OmniSharpSignatureHelpRequest request) =>
        await _mediator.Send(new GetSignatureHelpQuery(scriptId, request));

    [HttpPost("find-usages")]
    public async Task<OmniSharpQuickFixResponse?> FindUsages(Guid scriptId, [FromBody] OmniSharpFindUsagesRequest request) =>
        await _mediator.Send(new FindUsagesQuery(scriptId, request));

    [HttpGet("code-structure")]
    public async Task<CodeStructureResponse?> GetCodeStructure(Guid scriptId) =>
        await _mediator.Send(new GetCodeStructureQuery(scriptId));

    [HttpPost("code-check")]
    public async Task<OmniSharpQuickFixResponse?> CodeCheck(Guid scriptId, [FromBody] OmniSharpCodeCheckRequest request) =>
        await _mediator.Send(new CheckCodeQuery(scriptId, request));

    [HttpPost("inlay-hints")]
    public async Task<ActionResult<OmniSharpInlayHintResponse?>> GetInlayHints(Guid scriptId, [FromBody] OmniSharpInlayHintRequest request)
    {
        var response = await _mediator.Send(new GetInlayHintsQuery(scriptId, request));

        var serializationOptions = JsonSerializer.Configure(new JsonSerializerOptions()
        {
            IncludeFields = true
        });

        return new JsonResult(response, serializationOptions);
    }

    [HttpPost("inlay-hints/resolve")]
    public async Task<OmniSharpInlayHint?> ResolveInlayHint(Guid scriptId, [FromBody] InlayHintResolveRequest request) =>
        await _mediator.Send(new ResolveInlayHintQuery(scriptId, request));

    [HttpPost("code-actions")]
    public async Task<OmniSharpGetCodeActionsResponse?> GetCodeActions(Guid scriptId, [FromBody] OmniSharpGetCodeActionsRequest request) =>
        await _mediator.Send(new GetCodeActionsQuery(scriptId, request));

    [HttpPost("code-actions/run")]
    public async Task<ActionResult<RunCodeActionResponse?>> RunCodeAction(Guid scriptId, [FromBody] OmniSharpRunCodeActionRequest request) =>
        await _mediator.Send(new RunCodeActionCommand(scriptId, request));
}
