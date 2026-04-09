using System.Net;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class SetScriptConnectionToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task SetScriptConnection_SetConnection_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        var connId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/data-connection", HttpStatusCode.OK);

        var result = await SetScriptConnectionTool.SetScriptConnection(
            client, scriptId.ToString(), connId.ToString());

        Assert.Equal("Data connection set successfully.", result);
    }

    [Fact]
    public async Task SetScriptConnection_RemoveConnection_ReturnsRemovedMessage()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/data-connection", HttpStatusCode.OK);

        var result = await SetScriptConnectionTool.SetScriptConnection(
            client, scriptId.ToString(), dataConnectionId: null);

        Assert.Equal("Data connection removed from script.", result);
    }

    [Fact]
    public async Task SetScriptConnection_InvalidScriptId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await SetScriptConnectionTool.SetScriptConnection(client, "bad-id");

        Assert.Equal("Invalid scriptId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task SetScriptConnection_InvalidConnectionId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();
        var scriptId = Guid.NewGuid();

        var result = await SetScriptConnectionTool.SetScriptConnection(
            client, scriptId.ToString(), "bad-conn-id");

        Assert.Equal("Invalid dataConnectionId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task SetScriptConnection_SetConnection_IncludesConnectionIdInQuery()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        var connId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/data-connection", HttpStatusCode.OK);

        await SetScriptConnectionTool.SetScriptConnection(
            client, scriptId.ToString(), connId.ToString());

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains($"dataConnectionId={connId}", recorded.Url);
    }

    [Fact]
    public async Task SetScriptConnection_RemoveConnection_OmitsConnectionIdFromQuery()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/data-connection", HttpStatusCode.OK);

        await SetScriptConnectionTool.SetScriptConnection(
            client, scriptId.ToString(), dataConnectionId: null);

        var recorded = Assert.Single(handler.Requests);
        Assert.DoesNotContain("dataConnectionId", recorded.Url);
    }
}
