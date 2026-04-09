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
        handler.Setup(HttpMethod.Post, $"/headless/run/{scriptId}/gui", HttpStatusCode.OK,
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
    public async Task CreateScript_InvalidDataConnectionId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await CreateScriptTool.CreateScript(client, dataConnectionId: "not-a-guid");

        Assert.Equal("Invalid dataConnectionId format. Expected a GUID.", result);
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

    [Fact]
    public async Task CreateScript_WithConfigParams_SendsCorrectDto()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Patch, "/scripts/create", HttpStatusCode.OK,
            new ScriptDto { Id = Guid.NewGuid(), Name = "Test" });

        await CreateScriptTool.CreateScript(
            client,
            name: "Test",
            kind: "SQL",
            targetFrameworkVersion: "DotNet9",
            optimizationLevel: "Release",
            useAspNet: true,
            namespaces: new[] { "System.Numerics", "System.Net" });

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("\"kind\":\"SQL\"", recorded.Body);
        Assert.Contains("\"targetFrameworkVersion\":\"DotNet9\"", recorded.Body);
        Assert.Contains("\"optimizationLevel\":\"Release\"", recorded.Body);
        Assert.Contains("\"useAspNet\":true", recorded.Body);
        Assert.Contains("\"namespaces\":", recorded.Body);
        Assert.Contains("System.Numerics", recorded.Body);
        Assert.Contains("System.Net", recorded.Body);
    }

    [Fact]
    public async Task CreateScript_InvalidKind_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await CreateScriptTool.CreateScript(client, kind: "Invalid");

        Assert.Contains("Invalid kind", result);
        Assert.Contains("Program", result);
        Assert.Contains("SQL", result);
    }

    [Fact]
    public async Task CreateScript_InvalidOptimizationLevel_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await CreateScriptTool.CreateScript(client, optimizationLevel: "Fast");

        Assert.Contains("Invalid optimizationLevel", result);
        Assert.Contains("Debug", result);
        Assert.Contains("Release", result);
    }

    [Fact]
    public async Task CreateScript_WithOnlyKind_SendsDtoWithKindOnly()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Patch, "/scripts/create", HttpStatusCode.OK,
            new ScriptDto { Id = Guid.NewGuid(), Name = "Script 1" });

        await CreateScriptTool.CreateScript(client, kind: "Program");

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("\"kind\":\"Program\"", recorded.Body);
        Assert.Contains("\"targetFrameworkVersion\":null", recorded.Body);
        Assert.Contains("\"optimizationLevel\":null", recorded.Body);
        Assert.Contains("\"useAspNet\":null", recorded.Body);
        Assert.Contains("\"namespaces\":null", recorded.Body);
    }
}
