using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class OpenScriptToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    private static ScriptEnvironmentDto SampleEnvironment(Guid id) => new()
    {
        Script = new ScriptDto
        {
            Id = id,
            Name = "TestScript",
            Path = "/scripts/TestScript.netpad",
            Code = "Console.WriteLine(\"hello\");",
        },
        Status = "Ready"
    };

    [Fact]
    public async Task OpenScript_ById_ReturnsEnvironment()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        var env = SampleEnvironment(id);
        handler.Setup(HttpMethod.Patch, $"/session/open/{id}", HttpStatusCode.OK, env);

        var result = await OpenScriptTool.OpenScript(client, scriptId: id.ToString());

        var returned = JsonSerializer.Deserialize<ScriptEnvironmentDto>(result);
        Assert.NotNull(returned);
        Assert.Equal(id, returned.Script.Id);
        Assert.Equal("TestScript", returned.Script.Name);
        Assert.Single(handler.Requests);
        Assert.Contains($"/session/open/{id}", handler.Requests[0].Url);
    }

    [Fact]
    public async Task OpenScript_ByPath_ReturnsEnvironment()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        var env = SampleEnvironment(id);
        handler.Setup(HttpMethod.Patch, "/session/open/path", HttpStatusCode.OK, env);

        var result = await OpenScriptTool.OpenScript(client, path: "/scripts/TestScript.netpad");

        var returned = JsonSerializer.Deserialize<ScriptEnvironmentDto>(result);
        Assert.NotNull(returned);
        Assert.Equal(id, returned.Script.Id);
        Assert.Single(handler.Requests);
        Assert.Contains("/session/open/path", handler.Requests[0].Url);
        Assert.Contains("TestScript.netpad", handler.Requests[0].Body);
    }

    [Fact]
    public async Task OpenScript_InvalidGuid_ReturnsFormatError()
    {
        var (client, handler) = CreateClient();

        var result = await OpenScriptTool.OpenScript(client, scriptId: "not-a-guid");

        Assert.Contains("Invalid scriptId format", result);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task OpenScript_NeitherIdNorPath_ReturnsError()
    {
        var (client, handler) = CreateClient();

        var result = await OpenScriptTool.OpenScript(client);

        Assert.Contains("Either scriptId or path must be provided", result);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task OpenScript_ById_NotFound_PropagatesException()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/session/open/{id}", HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => OpenScriptTool.OpenScript(client, scriptId: id.ToString()));
    }
}
