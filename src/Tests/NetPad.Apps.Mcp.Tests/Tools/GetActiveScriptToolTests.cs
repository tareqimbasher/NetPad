using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class GetActiveScriptToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task GetActiveScript_NoActive_ReturnsMessage()
    {
        var (client, handler) = CreateClient();
        handler.SetupRaw(HttpMethod.Get, "/session/active", HttpStatusCode.OK, "null");

        var result = await GetActiveScriptTool.GetActiveScript(client, CancellationToken.None);

        Assert.Equal("No active script.", result);
    }

    [Fact]
    public async Task GetActiveScript_Active_ReturnsEnvironmentJson()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();

        handler.SetupRaw(HttpMethod.Get, "/session/active", HttpStatusCode.OK, $"\"{id}\"");
        handler.Setup(HttpMethod.Get, $"/session/environments/{id}", HttpStatusCode.OK,
            new ScriptEnvironmentDto
            {
                Script = new ScriptDto { Id = id, Name = "Active Script", Code = "var x = 1;" },
                Status = "Ready"
            });

        var result = await GetActiveScriptTool.GetActiveScript(client, CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        Assert.Equal("Active Script", root.GetProperty("script").GetProperty("name").GetString());
        Assert.Equal("Ready", root.GetProperty("status").GetString());
    }
}
