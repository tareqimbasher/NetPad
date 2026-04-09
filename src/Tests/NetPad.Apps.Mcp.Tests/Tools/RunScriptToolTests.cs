using System.Net;
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
    public async Task RunScript_ById_NotOpen_RunsHeadlessly()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, "/scripts/info", HttpStatusCode.OK, Array.Empty<ScriptInfoDto>());
        handler.Setup(HttpMethod.Post, $"/headless/run/{id}", HttpStatusCode.OK, SuccessResult());

        var result = await RunScriptTool.RunScript(client, scriptId: id.ToString());

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("/scripts/info", handler.Requests[0].Url);
        Assert.Contains(id.ToString(), handler.Requests[1].Url);
    }

    [Fact]
    public async Task RunScript_ByName_FindsAndRuns()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();

        handler.Setup(HttpMethod.Get, "/scripts/info", HttpStatusCode.OK,
            new[] { new ScriptInfoDto { Id = id, Name = "MyScript", Kind = "csharp", IsOpen = false } });
        handler.Setup(HttpMethod.Post, $"/headless/run/{id}", HttpStatusCode.OK, SuccessResult());

        var result = await RunScriptTool.RunScript(client, name: "MyScript");

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("/scripts/info", handler.Requests[0].Url);
        Assert.Contains(id.ToString(), handler.Requests[1].Url);
    }

    [Fact]
    public async Task RunScript_ByName_NotFound_ReturnsMessage()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/scripts/info", HttpStatusCode.OK, Array.Empty<ScriptInfoDto>());

        var result = await RunScriptTool.RunScript(client, name: "NonExistent");

        Assert.Contains("No script found", result);
        Assert.Contains("NonExistent", result);
    }

    [Fact]
    public async Task RunScript_ByName_MultipleMatches_ReturnsMessage()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/scripts/info", HttpStatusCode.OK, new[]
        {
            new ScriptInfoDto { Id = Guid.NewGuid(), Name = "Script A", Kind = "csharp", IsOpen = false },
            new ScriptInfoDto { Id = Guid.NewGuid(), Name = "Script AB", Kind = "csharp", IsOpen = false }
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
        handler.Setup(HttpMethod.Get, "/scripts/info", HttpStatusCode.OK, Array.Empty<ScriptInfoDto>());
        handler.Setup(HttpMethod.Post, $"/headless/run/{id}", HttpStatusCode.OK, SuccessResult());

        await RunScriptTool.RunScript(client, scriptId: id.ToString(), timeoutMs: 30000);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("timeoutMs=30000", handler.Requests[1].Url);
    }

    [Fact]
    public async Task RunScript_InvalidScriptId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await RunScriptTool.RunScript(client, scriptId: "not-a-guid");

        Assert.Equal("Invalid scriptId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task RunScript_ById_Open_RunsInGui()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, "/scripts/info", HttpStatusCode.OK,
            new[] { new ScriptInfoDto { Id = id, Name = "OpenScript", Kind = "csharp", IsOpen = true } });
        handler.Setup(HttpMethod.Post, $"/headless/run/{id}/gui", HttpStatusCode.OK, SuccessResult());

        await RunScriptTool.RunScript(client, scriptId: id.ToString());

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("/gui", handler.Requests[1].Url);
    }
}
