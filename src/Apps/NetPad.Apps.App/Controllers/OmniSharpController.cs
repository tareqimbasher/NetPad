using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.Services.OmniSharp;
using OmniSharp.Models;
using OmniSharp.Models.CodeFormat;
using OmniSharp.Models.FindImplementations;
using OmniSharp.Models.SemanticHighlight;
using OmniSharp.Models.SignatureHelp;
using OmniSharp.Models.v1.Completion;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Controllers;

[ApiController]
[Route("omnisharp/{scriptId:guid}")]
public class OmniSharpController : Controller
{
    private readonly OmniSharpServerCatalog _omniSharpServerCatalog;

    public OmniSharpController(OmniSharpServerCatalog omniSharpServerCatalog)
    {
        _omniSharpServerCatalog = omniSharpServerCatalog;
    }

    [HttpPost("completion")]
    public async Task<ActionResult<CompletionResponse?>> GetCompletion(Guid scriptId, [FromBody] CompletionRequest request)
    {
        var server = await GetOmniSharpServerAsync(scriptId);
        if (server == null)
        {
            return Ok(null);
        }

        var response = await server.OmniSharpServer.SendAsync<CompletionResponse>(new CompletionRequest
        {
            Line = server.Project.UserCodeStartsOnLine + request.Line,
            Column = request.Column,
            FileName = server.Project.ProgramFilePath,
            CompletionTrigger = request.CompletionTrigger,
            TriggerCharacter = request.TriggerCharacter
        });

        var serializationOptions = JsonSerializer.Configure(new JsonSerializerOptions()
        {
            IncludeFields = true
        });

        return new JsonResult(response, serializationOptions);
    }

    [HttpPost("completion/resolve")]
    public async Task<CompletionResolveResponse?> GetCompletionResolution(Guid scriptId, [FromBody] CompletionItemDto completionItemDto)
    {
        var server = await GetOmniSharpServerAsync(scriptId);
        if (server == null)
        {
            return null;
        }

        // Copy values from CompletionItemDto.Data property to base CompletionItem.Data Tuple property
        var completionItem = (CompletionItem)completionItemDto;
        if (completionItemDto.Data != null)
        {
            completionItem.Data = (completionItemDto.Data.Item1, completionItemDto.Data.Item2);
        }

        return await server.OmniSharpServer.SendAsync<CompletionResolveResponse>(new CompletionResolveRequest
        {
            Item = completionItem
        });
    }

    [HttpPost("format-code")]
    public async Task<CodeFormatResponse?> FormatCode(Guid scriptId, [FromBody] CodeFormatRequest request)
    {
        var server = await GetOmniSharpServerAsync(scriptId);
        if (server == null)
        {
            return null;
        }

        return await server.OmniSharpServer.SendAsync<CodeFormatResponse>(new CodeFormatRequest()
        {
            Buffer = request.Buffer,
            FileName = server.Project.ProgramFilePath
        });
    }

    [HttpPost("semantic-highlights")]
    public async Task<SemanticHighlightResponse?> GetSemanticHighlights(Guid scriptId, [FromBody] SemanticHighlightRequest request)
    {
        var server = await GetOmniSharpServerAsync(scriptId);
        if (server == null)
        {
            return null;
        }

        var response = await server.OmniSharpServer.SendAsync<SemanticHighlightResponse>(new SemanticHighlightRequest()
        {
            FileName = server.Project.ProgramFilePath
        });

        if (response != null)
        {
            // Remove any spans before user code
            response.Spans = response.Spans.Where(s => s.StartLine >= server.Project.UserCodeStartsOnLine).ToArray();

            // Adjust line numbers
            foreach (var span in response.Spans)
            {
                span.StartLine -= server.Project.UserCodeStartsOnLine;
                span.EndLine -= server.Project.UserCodeStartsOnLine;
            }
        }

        return response;
    }

    [HttpPost("find-implementations")]
    public async Task<QuickFixResponse?> FindImplementations(Guid scriptId, [FromBody] FindImplementationsRequest request)
    {
        var server = await GetOmniSharpServerAsync(scriptId);
        if (server == null)
        {
            return null;
        }

        request.FileName = server.Project.ProgramFilePath;
        request.Line = request.Line + server.Project.UserCodeStartsOnLine - 1;

        var response = await server.OmniSharpServer.SendAsync<QuickFixResponse>(request);

        if (response == null)
        {
            return response;
        }

        foreach (var quickFix in response.QuickFixes)
        {
            quickFix.Line = quickFix.Line - server.Project.UserCodeStartsOnLine + 1;
            quickFix.EndLine = quickFix.EndLine - server.Project.UserCodeStartsOnLine + 1;
        }

        return response;
    }

    [HttpPost("quick-info")]
    public async Task<QuickInfoResponse?> GetQuickInfo(Guid scriptId, [FromBody] QuickInfoRequest request)
    {
        var server = await GetOmniSharpServerAsync(scriptId);
        if (server == null)
        {
            return null;
        }

        request.FileName = server.Project.ProgramFilePath;
        request.Line = request.Line + server.Project.UserCodeStartsOnLine - 1;

        return await server.OmniSharpServer.SendAsync<QuickInfoResponse>(request);
    }

    [HttpPost("signature-help")]
    public async Task<SignatureHelpResponse?> GetSignatureHelp(Guid scriptId, [FromBody] SignatureHelpRequest request)
    {
        var server = await GetOmniSharpServerAsync(scriptId);
        if (server == null)
        {
            return null;
        }

        request.FileName = server.Project.ProgramFilePath;
        request.Line = request.Line + server.Project.UserCodeStartsOnLine - 1;

        return await server.OmniSharpServer.SendAsync<SignatureHelpResponse>(request);
    }


    private async Task<AppOmniSharpServer?> GetOmniSharpServerAsync(Guid scriptId)
    {
        return await _omniSharpServerCatalog.GetOmniSharpServerAsync(scriptId);
    }

    /// <summary>
    /// Used to bind <see cref="CompletionItem"/> when posting it to API endpoint. STJ does not know
    /// how to deserialize {item1: 0, item2: 1} into a ValueTuple(long, int)
    /// </summary>
    public class CompletionItemDto : CompletionItem
    {
        /// <summary>
        /// Hides base Data property
        /// </summary>
        public new CompletionItemData? Data { get; set; }

        public class CompletionItemData
        {
            public long Item1 { get; set; }
            public int Item2 { get; set; }
        }
    }
}
