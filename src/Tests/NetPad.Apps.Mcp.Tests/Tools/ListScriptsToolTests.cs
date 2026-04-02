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
    public async Task ListScripts_MergesOpenAndSaved_DeduplicatesOpen()
    {
        var (client, handler) = CreateClient();
        var openId = Guid.NewGuid();
        var savedId = Guid.NewGuid();

        handler.Setup(HttpMethod.Get, "/session/environments", HttpStatusCode.OK, new[]
        {
            new ScriptEnvironmentDto
            {
                Script = new ScriptDto { Id = openId, Name = "Open Script" },
                Status = "Running",
                RunDurationMilliseconds = 1234
            }
        });
        handler.Setup(HttpMethod.Get, "/scripts", HttpStatusCode.OK, new[]
        {
            // This one is also open — should NOT appear in Saved
            new ScriptSummaryDto { Id = openId, Name = "Open Script", Kind = "csharp" },
            // This one is only saved — should appear in Saved
            new ScriptSummaryDto { Id = savedId, Name = "Saved Only", Kind = "csharp" }
        });

        var result = await ListScriptsTool.ListScripts(client, CancellationToken.None);
        var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        // Open section
        var open = root.GetProperty("Open");
        Assert.Equal(1, open.GetArrayLength());
        Assert.Equal("Open Script", open[0].GetProperty("Name").GetString());
        Assert.Equal("Running", open[0].GetProperty("Status").GetString());

        // Saved section (deduplicated)
        var saved = root.GetProperty("Saved");
        Assert.Equal(1, saved.GetArrayLength());
        Assert.Equal("Saved Only", saved[0].GetProperty("Name").GetString());
    }

    [Fact]
    public async Task ListScripts_NoScripts_ReturnsEmptyArrays()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/session/environments", HttpStatusCode.OK,
            Array.Empty<ScriptEnvironmentDto>());
        handler.Setup(HttpMethod.Get, "/scripts", HttpStatusCode.OK,
            Array.Empty<ScriptSummaryDto>());

        var result = await ListScriptsTool.ListScripts(client, CancellationToken.None);
        var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        Assert.Equal(0, root.GetProperty("Open").GetArrayLength());
        Assert.Equal(0, root.GetProperty("Saved").GetArrayLength());
    }
}
