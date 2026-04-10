using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class GetDataConnectionToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task GetDataConnection_ValidId_ReturnsConnectionDetails()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        var responseJson = JsonSerializer.Serialize(new
        {
            id = connId,
            name = "MyPostgres",
            type = "PostgreSQL",
            host = "localhost",
            port = "5432",
            databaseName = "myapp"
        });
        handler.SetupRaw(HttpMethod.Get, $"/data-connections/{connId}", HttpStatusCode.OK, responseJson);

        var result = await GetDataConnectionTool.GetDataConnection(client, connId.ToString(), CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        Assert.Equal("MyPostgres", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal("PostgreSQL", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("localhost", doc.RootElement.GetProperty("host").GetString());
        Assert.Equal("5432", doc.RootElement.GetProperty("port").GetString());
        Assert.Equal("myapp", doc.RootElement.GetProperty("databaseName").GetString());
    }

    [Fact]
    public async Task GetDataConnection_ServerId_FallsBackToServerEndpoint()
    {
        var (client, handler) = CreateClient();
        var serverId = Guid.NewGuid();
        handler.SetupRaw(HttpMethod.Get, $"/data-connections/{serverId}", HttpStatusCode.OK, "");
        var serverJson = JsonSerializer.Serialize(new
        {
            id = serverId,
            name = "Blink STG",
            type = "PostgreSQL",
            host = "stg.example.com"
        });
        handler.SetupRaw(HttpMethod.Get, $"/data-connections/servers/{serverId}", HttpStatusCode.OK, serverJson);

        var result = await GetDataConnectionTool.GetDataConnection(client, serverId.ToString(), CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        Assert.Equal("Blink STG", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal("PostgreSQL", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("stg.example.com", doc.RootElement.GetProperty("host").GetString());
    }

    [Fact]
    public async Task GetDataConnection_InvalidId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await GetDataConnectionTool.GetDataConnection(client, "not-a-guid", CancellationToken.None);

        Assert.Equal("Invalid connectionId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task GetDataConnection_NotFound_ReturnsNotFoundMessage()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        handler.SetupRaw(HttpMethod.Get, $"/data-connections/{connId}", HttpStatusCode.OK, "");
        handler.SetupRaw(HttpMethod.Get, $"/data-connections/servers/{connId}", HttpStatusCode.OK, "");

        var result = await GetDataConnectionTool.GetDataConnection(client, connId.ToString(), CancellationToken.None);

        Assert.Equal($"Data connection or server not found: {connId}", result);
    }

    [Fact]
    public async Task GetDataConnection_ConnectionFound_DoesNotCallServerEndpoint()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        handler.SetupRaw(HttpMethod.Get, $"/data-connections/{connId}", HttpStatusCode.OK,
            JsonSerializer.Serialize(new { id = connId, name = "Test" }));

        await GetDataConnectionTool.GetDataConnection(client, connId.ToString(), CancellationToken.None);

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, recorded.Method);
        Assert.Contains($"/data-connections/{connId}", recorded.Url);
        Assert.DoesNotContain("/servers/", recorded.Url);
    }

    [Fact]
    public async Task GetDataConnection_StripsSensitiveProperties()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        var responseJson = JsonSerializer.Serialize(new
        {
            id = connId,
            name = "MyDB",
            type = "PostgreSQL",
            host = "localhost",
            userId = "admin",
            password = "secret123"
        });
        handler.SetupRaw(HttpMethod.Get, $"/data-connections/{connId}", HttpStatusCode.OK, responseJson);

        var result = await GetDataConnectionTool.GetDataConnection(client, connId.ToString(), CancellationToken.None);

        Assert.DoesNotContain("userId", result);
        Assert.DoesNotContain("admin", result);
        Assert.DoesNotContain("password", result);
        Assert.DoesNotContain("secret123", result);
        var doc = JsonDocument.Parse(result);
        Assert.Equal("MyDB", doc.RootElement.GetProperty("name").GetString());
    }
}
