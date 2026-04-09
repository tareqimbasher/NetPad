using System.Net;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class CloseScriptToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task CloseScript_ValidId_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/session/{id}/close", HttpStatusCode.OK);

        var result = await CloseScriptTool.CloseScript(client, id.ToString(), CancellationToken.None);

        Assert.Equal("Script closed.", result);
        var recorded = Assert.Single(handler.Requests);
        Assert.Contains($"/session/{id}/close", recorded.Url);
        Assert.Contains("discardUnsavedChanges=true", recorded.Url);
    }

    [Fact]
    public async Task CloseScript_InvalidGuid_ReturnsFormatError()
    {
        var (client, handler) = CreateClient();

        var result = await CloseScriptTool.CloseScript(client, "not-a-guid", CancellationToken.None);

        Assert.Contains("Invalid scriptId format", result);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task CloseScript_ScriptNotOpen_ReturnsNotOpenMessage()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/session/{id}/close", HttpStatusCode.NotFound);

        var result = await CloseScriptTool.CloseScript(client, id.ToString(), CancellationToken.None);

        Assert.Equal("Script is not open.", result);
    }

    [Fact]
    public async Task CloseScript_ServerError_PropagatesException()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/session/{id}/close", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CloseScriptTool.CloseScript(client, id.ToString(), CancellationToken.None));
    }
}
