using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class GetScriptStatusToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task GetScriptStatus_ValidId_ReturnsStatus()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, $"/session/environments/{id}/status", HttpStatusCode.OK,
            new ScriptStatusDto { ScriptId = id, Name = "Test", Status = "Running" });

        var result = await GetScriptStatusTool.GetScriptStatus(client, id.ToString(), CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        Assert.Equal("Running", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("Test", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetScriptStatus_InvalidId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await GetScriptStatusTool.GetScriptStatus(client, "not-valid", CancellationToken.None);

        Assert.Equal("Invalid scriptId format. Expected a GUID.", result);
    }
}
