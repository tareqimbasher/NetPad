using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class RunScriptToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    private static HeadlessRunResult SuccessResult()
    {
        return new HeadlessRunResult { Status = "completed", Success = true, DurationMs = 50 };
    }

    [Fact]
    public async Task RunScript_ById_CallsRunScriptAsync()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Post, $"/api/headless/run/{id}", HttpStatusCode.OK, SuccessResult());

        var result = await RunScriptTool.RunScript(client, scriptId: id.ToString());

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains(id.ToString(), recorded.Url);
    }

    [Fact]
    public async Task RunScript_ByName_FindsAndRuns()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();

        handler.Setup(HttpMethod.Get, "/scripts/find", HttpStatusCode.OK,
            new[] { new ScriptSummaryDto { Id = id, Name = "MyScript", Kind = "csharp" } });
        handler.Setup(HttpMethod.Post, $"/api/headless/run/{id}", HttpStatusCode.OK, SuccessResult());

        var result = await RunScriptTool.RunScript(client, name: "MyScript");

        // Should have made two calls: find + run
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("/scripts/find", handler.Requests[0].Url);
        Assert.Contains(id.ToString(), handler.Requests[1].Url);
    }

    [Fact]
    public async Task RunScript_ByName_NotFound_ReturnsMessage()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/scripts/find", HttpStatusCode.OK, Array.Empty<ScriptSummaryDto>());

        var result = await RunScriptTool.RunScript(client, name: "NonExistent");

        Assert.Contains("No script found", result);
        Assert.Contains("NonExistent", result);
    }

    [Fact]
    public async Task RunScript_ByName_MultipleMatches_ReturnsMessage()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/scripts/find", HttpStatusCode.OK, new[]
        {
            new ScriptSummaryDto { Id = Guid.NewGuid(), Name = "Script A", Kind = "csharp" },
            new ScriptSummaryDto { Id = Guid.NewGuid(), Name = "Script AB", Kind = "csharp" }
        });

        var result = await RunScriptTool.RunScript(client, name: "Script");

        Assert.Contains("Multiple scripts match", result);
        Assert.Contains("Script A", result);
        Assert.Contains("Script AB", result);
    }

    [Fact]
    public async Task RunScript_NoIdOrName_ReturnsError()
    {
        var (client, _) = CreateClient();

        var result = await RunScriptTool.RunScript(client);

        Assert.Contains("Either scriptId or name must be provided", result);
    }

    [Fact]
    public async Task RunScript_WithTimeout_PassesTimeoutThrough()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Post, $"/api/headless/run/{id}", HttpStatusCode.OK, SuccessResult());

        await RunScriptTool.RunScript(client, scriptId: id.ToString(), timeoutMs: 30000);

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("timeoutMs=30000", recorded.Url);
    }
}
