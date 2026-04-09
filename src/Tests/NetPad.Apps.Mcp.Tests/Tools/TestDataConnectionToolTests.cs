using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class TestDataConnectionToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task TestDataConnection_ValidId_Success_ReturnsResult()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/data-connections/{connId}/test", HttpStatusCode.OK,
            new DataConnectionTestResultDto { Success = true, Message = "Connection successful" });

        var result = await TestDataConnectionTool.TestDataConnection(client, connId.ToString(), CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Connection successful", doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task TestDataConnection_ValidId_Failure_ReturnsResult()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/data-connections/{connId}/test", HttpStatusCode.OK,
            new DataConnectionTestResultDto { Success = false, Message = "Connection refused" });

        var result = await TestDataConnectionTool.TestDataConnection(client, connId.ToString(), CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Connection refused", doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task TestDataConnection_InvalidId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await TestDataConnectionTool.TestDataConnection(client, "invalid", CancellationToken.None);

        Assert.Equal("Invalid connectionId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task TestDataConnection_CallsCorrectEndpoint()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/data-connections/{connId}/test", HttpStatusCode.OK,
            new DataConnectionTestResultDto { Success = true });

        await TestDataConnectionTool.TestDataConnection(client, connId.ToString(), CancellationToken.None);

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, recorded.Method);
        Assert.Contains($"/data-connections/{connId}/test", recorded.Url);
    }
}
