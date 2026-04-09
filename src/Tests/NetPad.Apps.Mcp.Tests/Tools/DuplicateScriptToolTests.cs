using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class DuplicateScriptToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task DuplicateScript_ValidId_ReturnsDuplicatedScript()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/scripts/{scriptId}/duplicate", HttpStatusCode.OK,
            new ScriptDto { Id = newId, Name = "Script 1 (Copy)" });

        var result = await DuplicateScriptTool.DuplicateScript(client, scriptId.ToString(), CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        Assert.Equal(newId.ToString(), doc.RootElement.GetProperty("id").GetString());
        Assert.Equal("Script 1 (Copy)", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task DuplicateScript_InvalidId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await DuplicateScriptTool.DuplicateScript(client, "not-a-guid", CancellationToken.None);

        Assert.Equal("Invalid scriptId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task DuplicateScript_CallsCorrectEndpoint()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/scripts/{scriptId}/duplicate", HttpStatusCode.OK,
            new ScriptDto { Id = Guid.NewGuid(), Name = "Copy" });

        await DuplicateScriptTool.DuplicateScript(client, scriptId.ToString(), CancellationToken.None);

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, recorded.Method);
        Assert.Contains($"/scripts/{scriptId}/duplicate", recorded.Url);
    }
}
