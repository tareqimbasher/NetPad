using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetPad.Application;
using NetPad.Plugins.OmniSharp.Services;
using NetPad.Sessions;
using OmniSharp.Models;
using OmniSharp.Models.CodeCheck;
using OmniSharp.Models.CodeFormat;
using OmniSharp.Models.FindImplementations;
using OmniSharp.Models.FindUsages;
using OmniSharp.Models.SemanticHighlight;
using OmniSharp.Models.SignatureHelp;
using OmniSharp.Models.v1.Completion;
using OmniSharp.Models.V2;
using OmniSharp.Models.V2.CodeStructure;
using ISession = NetPad.Sessions.ISession;
using JsonSerializer = NetPad.Common.JsonSerializer;
using Range2 = OmniSharp.Models.V2.Range;
using OmniSharpInlayHints = OmniSharp.Models.v1.InlayHints;

namespace NetPad.Plugins.OmniSharp;

[ApiController]
[Route("omnisharp/{scriptId:guid}")]
public class OmniSharpController : Controller
{
    private readonly OmniSharpServerCatalog _omniSharpServerCatalog;

    public OmniSharpController(OmniSharpServerCatalog omniSharpServerCatalog)
    {
        _omniSharpServerCatalog = omniSharpServerCatalog;
    }

    [HttpPatch("restart-server")]
    public async Task<bool> RestartServer(
        Guid scriptId,
        [FromServices] ISession session,
        [FromServices] IAppStatusMessagePublisher appStatusMessagePublisher)
    {
        var environment = session.Get(scriptId);
        if (environment == null)
        {
            throw new Exception($"Could not find script with ID: {scriptId}");
        }

        string scriptName = environment.Script.Name;

        var server = await GetOmniSharpServerAsync(scriptId);
        if (server == null)
        {
            return false;
        }

        var result = await server.RestartAsync((progress) =>
        {
            appStatusMessagePublisher.PublishAsync(scriptId, progress);
        });

        await appStatusMessagePublisher.PublishAsync(scriptId, $"{(result ? "Restarted" : "Failed to restart")} OmniSharp Server");

        return result;
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

    [HttpPost("completion/after-insert")]
    public async Task<CompletionAfterInsertResponse?> GetCompletionAfterInsert(Guid scriptId, [FromBody] CompletionItemDto completionItemDto)
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

        return await server.OmniSharpServer.SendAsync<CompletionAfterInsertResponse>(new CompletionAfterInsertRequest
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

        if (response == null || response.QuickFixes == null)
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

    [HttpPost("find-usages")]
    public async Task<QuickFixResponse?> FindUsages(Guid scriptId, [FromBody] FindUsagesRequest request)
    {
        var server = await GetOmniSharpServerAsync(scriptId);
        if (server == null)
        {
            return null;
        }

        request.FileName = server.Project.ProgramFilePath;
        request.Line = request.Line + server.Project.UserCodeStartsOnLine - 1;

        var response = await server.OmniSharpServer.SendAsync<QuickFixResponse>(request);

        if (response == null || response.QuickFixes == null)
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

    [HttpGet("code-structure")]
    public async Task<CodeStructureResponse?> GetCodeStructure(Guid scriptId)
    {
        var server = await GetOmniSharpServerAsync(scriptId);
        if (server == null)
        {
            return null;
        }

        var response = await server.OmniSharpServer.SendAsync<CodeStructureResponse>(new CodeStructureRequest()
        {
            FileName = server.Project.ProgramFilePath
        });

        if (response == null)
        {
            return response;
        }

        // Correct line numbers
        int userCodeStartsOnLine = server.Project.UserCodeStartsOnLine;
        RecurseCodeElements(response.Elements, null, (element, parent) =>
        {
            if (!element.Ranges.TryGetValue("name", out Range2? range)) return;

            element.Ranges["name"] = new Range2()
            {
                Start = new Point()
                {
                    Line = range.Start.Line - userCodeStartsOnLine + 1,
                    Column = range.Start.Column
                },
                End = new Point()
                {
                    Line = range.End.Line - userCodeStartsOnLine + 1,
                    Column = range.End.Column
                }
            };
        });

        return response;
    }

    [HttpPost("code-check")]
    public async Task<QuickFixResponse?> CodeCheck(Guid scriptId, [FromBody] CodeCheckRequest request)
    {
        var server = await GetOmniSharpServerAsync(scriptId);
        if (server == null)
        {
            return null;
        }

        request.FileName = server.Project.ProgramFilePath;

        var response = await server.OmniSharpServer.SendAsync<QuickFixResponse>(request);

        if (response == null || response.QuickFixes == null)
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

    [HttpPost("inlay-hints")]
    public async Task<ActionResult<OmniSharpInlayHints.InlayHintResponse?>> GetInlayHints(Guid scriptId, [FromBody] OmniSharpInlayHints.InlayHintRequest request)
    {
        var server = await GetOmniSharpServerAsync(scriptId);
        if (server == null || request.Location == null)
        {
            return null;
        }

        request = new OmniSharpInlayHints.InlayHintRequest()
        {
            Location = new Location()
            {
                FileName = server.Project.ProgramFilePath,
                Range = new Range2()
                {
                    Start = new Point()
                    {
                        Line = request.Location.Range.Start.Line + server.Project.UserCodeStartsOnLine,
                        Column = request.Location.Range.Start.Column
                    },
                    End = new Point()
                    {
                        Line = request.Location.Range.End.Line + server.Project.UserCodeStartsOnLine,
                        Column = request.Location.Range.End.Column
                    }
                }
            }
        };

        var response = await server.OmniSharpServer.SendAsync<OmniSharpInlayHints.InlayHintResponse>(request);

        if (response == null)
        {
            return response;
        }

        foreach (var inlayHint in response.InlayHints)
        {
            inlayHint.Position = new Point()
            {
                Line = inlayHint.Position.Line - server.Project.UserCodeStartsOnLine + 1,
                Column = inlayHint.Position.Column
            };
        }

        var serializationOptions = JsonSerializer.Configure(new JsonSerializerOptions()
        {
            IncludeFields = true
        });

        return new JsonResult(response, serializationOptions);
    }

    [HttpPost("inlay-hints/resolve")]
    public async Task<OmniSharpInlayHints.InlayHint?> ResolveInlayHint(Guid scriptId, [FromBody] InlayHintResolveRequest request)
    {
        var server = await GetOmniSharpServerAsync(scriptId);
        if (server == null)
        {
            return null;
        }

        request.Hint.Position = new Point()
        {
            Line = request.Hint.Position.Line + server.Project.UserCodeStartsOnLine - 1,
            Column = request.Hint.Position.Column
        };

        var response = await server.OmniSharpServer.SendAsync<OmniSharpInlayHints.InlayHint>(new OmniSharpInlayHints.InlayHintResolveRequest()
        {
            Hint = new OmniSharpInlayHints.InlayHint()
            {
                Label = request.Hint.Label,
                Tooltip = request.Hint.Tooltip,
                Position = request.Hint.Position,
                Data = (request.Hint.Data.Item1, request.Hint.Data.Item2)
            }
        });

        if (response == null)
        {
            return response;
        }

        response.Position = new Point()
        {
            Line = response.Position.Line - server.Project.UserCodeStartsOnLine + 1,
            Column = response.Position.Column
        };

        return response;
    }


    private void RecurseCodeElements(
        IEnumerable<CodeStructureResponse.CodeElement> elements,
        CodeStructureResponse.CodeElement? parent,
        Action<CodeStructureResponse.CodeElement, CodeStructureResponse.CodeElement?> action)
    {
        foreach (var element in elements)
        {
            action(element, parent);

            if (element.Children?.Any() == true)
            {
                RecurseCodeElements(element.Children, element, action);
            }
        }
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

    /// <summary>
    /// Used to bind <see cref="InlayHint"/> when posting it to API endpoint. STJ does not know
    /// how to deserialize {item1: 0, item2: 1} into a ValueTuple(long, int)
    /// </summary>
    public record InlayHintResolveRequest : OmniSharpInlayHints.InlayHintResolveRequest
    {
        /// <summary>
        /// Hides base Hint property
        /// </summary>
        public new InlayHint Hint { get; set; }

        public class InlayHint
        {
            public Point Position { get; set; }
            public string Label { get; set; }
            public string? Tooltip { get; set; }
            public InlayHintData Data { get; set; }
        }

        public class InlayHintData
        {
            public string Item1 { get; set; }
            public int Item2 { get; set; }
        }
    }

    /// <summary>
    /// Used to be able to deserialize CodeElement type from OmniSharp Server return
    /// The original CodeElement from OmniSharp.Models also uses some readonly properties
    /// that we need to manipulate (ie. Ranges)
    /// </summary>
    public class CodeStructureResponse
    {
        public IReadOnlyList<CodeElement> Elements { get; set; }

        public class CodeElement
        {
            public CodeElement(
                string kind,
                string name,
                string displayName,
                IReadOnlyList<CodeElement> children,
                Dictionary<string, Range2> ranges,
                IReadOnlyDictionary<string, object> properties)
            {
                Kind = kind;
                Name = name;
                DisplayName = displayName;
                Children = children;
                Ranges = ranges;
                Properties = properties;
            }

            public string Kind { get; }
            public string Name { get; }
            public string DisplayName { get; }
            public IReadOnlyList<CodeElement> Children { get; }
            public Dictionary<string, Range2> Ranges { get; }
            public IReadOnlyDictionary<string, object> Properties { get; }

            public override string ToString()
                => $"{Kind} {Name}";
        }
    }
}
