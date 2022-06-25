using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.Services;
using OmniSharp.Models.v1.Completion;
using JsonSerializer = NetPad.Common.JsonSerializer;

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
    public async Task<ActionResult<CompletionResponse?>> GetCompletion(Guid scriptId, [FromBody] CompletionRequest request)
    {
        var server = GetOmniSharpServer(scriptId);
        if (server == null)
        {
            return Ok(null);
        }

        var response = await server.Send<CompletionResponse>(new CompletionRequest
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

    [HttpPost("{scriptId:guid}/completion/resolve")]
    public async Task<CompletionResolveResponse?> GetCompletionResolution(Guid scriptId, [FromBody] AppCompletionItem appCompletionItem)
    {
        var server = GetOmniSharpServer(scriptId);
        if (server == null)
        {
            return null;
        }

        // Copy values from AppCompletionItem.Data property to base CompletionItem.Data Tuple property
        var completionItem = (CompletionItem)appCompletionItem;
        if (appCompletionItem.Data != null)
        {
            completionItem.Data = (appCompletionItem.Data.Item1, appCompletionItem.Data.Item2);
        }

        return await server.Send<CompletionResolveResponse>(new CompletionResolveRequest
        {
            Item = completionItem
        });
    }

    private AppOmniSharpServer? GetOmniSharpServer(Guid scriptId)
    {
        return _omniSharpServerCatalog.GetOmniSharpServer(scriptId);
    }

    /// <summary>
    /// Used to bind <see cref="CompletionItem"/> when posting it to API endpoint. STJ does not know
    /// how to deserialize {item1: 0, item2: 1} into a ValueTuple(long, int)
    /// </summary>
    public class AppCompletionItem : CompletionItem
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
