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

        var result = await CreateScriptTool.CreateScript(
            client,
            name: "Test Script",
            code: "var x = 1;",
            dataConnectionId: connId.ToString(),
            runImmediately: true);

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("\"name\":\"Test Script\"", recorded.Body);
        Assert.Contains("\"code\":\"var x = 1;\"", recorded.Body);
        Assert.Contains(connId.ToString(), recorded.Body);
        Assert.Contains("\"runImmediately\":true", recorded.Body);

        var doc = JsonDocument.Parse(result);
        Assert.Equal("Test Script", doc.RootElement.GetProperty("name").GetString());
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
