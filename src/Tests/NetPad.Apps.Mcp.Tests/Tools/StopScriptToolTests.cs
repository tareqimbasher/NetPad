using System.Net;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class StopScriptToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task StopScript_ValidId_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/scripts/{id}/stop", HttpStatusCode.OK);

        var result = await StopScriptTool.StopScript(client, id.ToString(), CancellationToken.None);

        Assert.Equal("Script stop requested.", result);
        Assert.Single(handler.Requests);
        Assert.Contains($"/scripts/{id}/stop", handler.Requests[0].Url);
    }

    [Fact]
    public async Task StopScript_InvalidGuid_ReturnsFormatError()
    {
        var (client, handler) = CreateClient();

        var result = await StopScriptTool.StopScript(client, "not-a-guid", CancellationToken.None);

        Assert.Contains("Invalid scriptId format", result);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task StopScript_ServerError_PropagatesException()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/scripts/{id}/stop", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => StopScriptTool.StopScript(client, id.ToString(), CancellationToken.None));
    }
}
