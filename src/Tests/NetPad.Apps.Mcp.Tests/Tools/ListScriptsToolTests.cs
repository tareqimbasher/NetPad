using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class ListScriptsToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task ListScripts_ReturnsOpenAndSavedScripts()
    {
        var (client, handler) = CreateClient();
        var openId = Guid.NewGuid();
        var savedId = Guid.NewGuid();

        handler.Setup(HttpMethod.Get, "/scripts/info", HttpStatusCode.OK, new[]
        {
            new ScriptInfoDto { Id = openId, Name = "Open Script", Kind = "csharp", IsOpen = true, Status = "Running", RunDurationMilliseconds = 1234 },
            new ScriptInfoDto { Id = savedId, Name = "Saved Only", Kind = "csharp", IsOpen = false }
        });

        var result = await ListScriptsTool.ListScripts(client, CancellationToken.None);
        var doc = JsonDocument.Parse(result);
        var arr = doc.RootElement;

        Assert.Equal(2, arr.GetArrayLength());
        Assert.Equal("Open Script", arr[0].GetProperty("Name").GetString());
        Assert.True(arr[0].GetProperty("IsOpen").GetBoolean());
        Assert.Equal("Running", arr[0].GetProperty("Status").GetString());
        Assert.Equal("Saved Only", arr[1].GetProperty("Name").GetString());
        Assert.False(arr[1].GetProperty("IsOpen").GetBoolean());
    }

    [Fact]
    public async Task ListScripts_NoScripts_ReturnsMessage()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/scripts/info", HttpStatusCode.OK, Array.Empty<ScriptInfoDto>());

        var result = await ListScriptsTool.ListScripts(client, CancellationToken.None);

        Assert.Equal("No scripts found.", result);
    }
}
