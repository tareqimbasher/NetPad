using System.Net;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class RefreshDataConnectionToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task RefreshDataConnection_ValidId_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/data-connections/{connId}/refresh", HttpStatusCode.OK);

        var result = await RefreshDataConnectionTool.RefreshDataConnection(client, connId.ToString(), CancellationToken.None);

        Assert.Contains(connId.ToString(), result);
        Assert.Contains("refresh initiated", result);
    }

    [Fact]
    public async Task RefreshDataConnection_InvalidId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await RefreshDataConnectionTool.RefreshDataConnection(client, "bad-id", CancellationToken.None);

        Assert.Equal("Invalid connectionId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task RefreshDataConnection_CallsCorrectEndpoint()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/data-connections/{connId}/refresh", HttpStatusCode.OK);

        await RefreshDataConnectionTool.RefreshDataConnection(client, connId.ToString(), CancellationToken.None);

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, recorded.Method);
        Assert.Contains($"/data-connections/{connId}/refresh", recorded.Url);
    }
}
