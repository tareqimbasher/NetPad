using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class FindScriptToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task FindScript_NoResults_ReturnsNotFoundMessage()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/scripts/find", HttpStatusCode.OK, Array.Empty<ScriptSummaryDto>());

        var result = await FindScriptTool.FindScript(client, "NonExistent", CancellationToken.None);

        Assert.Contains("No scripts found", result);
        Assert.Contains("NonExistent", result);
    }

    [Fact]
    public async Task FindScript_WithResults_ReturnsJsonArray()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, "/scripts/find", HttpStatusCode.OK, new[]
        {
            new ScriptSummaryDto { Id = id, Name = "MyScript", Kind = "csharp", Path = "/scripts/MyScript.netpad" }
        });

        var result = await FindScriptTool.FindScript(client, "My", CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        var arr = doc.RootElement;
        Assert.Equal(1, arr.GetArrayLength());
        Assert.Equal("MyScript", arr[0].GetProperty("name").GetString());
    }
}
