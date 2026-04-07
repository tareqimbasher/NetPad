using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class CreateScriptToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task CreateScript_WithAllParams_SendsCorrectDto()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        var scriptId = Guid.NewGuid();

        handler.Setup(HttpMethod.Patch, "/scripts/create", HttpStatusCode.OK,
            new ScriptDto { Id = scriptId, Name = "Test Script", Code = "var x = 1;" });
        handler.Setup(HttpMethod.Post, $"/api/headless/run/{scriptId}/gui", HttpStatusCode.OK,
            new HeadlessRunResult { Status = "completed", Success = true, DurationMs = 50 });

        var result = await CreateScriptTool.CreateScript(
            client,
            name: "Test Script",
            code: "var x = 1;",
            dataConnectionId: connId.ToString(),
            runImmediately: true);

        // First request is create, second is GUI run
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("\"name\":\"Test Script\"", handler.Requests[0].Body);
        Assert.Contains("\"code\":\"var x = 1;\"", handler.Requests[0].Body);
        Assert.Contains(connId.ToString(), handler.Requests[0].Body);

        var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("Script", out _));
        Assert.True(doc.RootElement.TryGetProperty("RunResult", out _));
    }

    [Fact]
    public async Task CreateScript_NoParams_SendsMinimalDto()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Patch, "/scripts/create", HttpStatusCode.OK,
            new ScriptDto { Id = Guid.NewGuid(), Name = "Script 1" });

        await CreateScriptTool.CreateScript(client);

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("\"runImmediately\":false", recorded.Body);
    }
}
