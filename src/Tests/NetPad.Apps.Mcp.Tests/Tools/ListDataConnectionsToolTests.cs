using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class ListDataConnectionsToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task ListDataConnections_ReturnsConnectionList()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, "/data-connections", HttpStatusCode.OK,
            new GetAllConnectionsResponse
            {
                Connections = [new DataConnectionDto { Id = connId, Name = "MyDB", Type = "PostgreSQL" }],
                Servers = []
            });

        var result = await ListDataConnectionsTool.ListDataConnections(client, CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        var connections = doc.RootElement.GetProperty("connections");
        Assert.Equal(1, connections.GetArrayLength());
        Assert.Equal("MyDB", connections[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task ListDataConnections_NoConnections_ReturnsEmptyArrays()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/data-connections", HttpStatusCode.OK,
            new GetAllConnectionsResponse { Connections = [], Servers = [] });

        var result = await ListDataConnectionsTool.ListDataConnections(client, CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        Assert.Equal(0, doc.RootElement.GetProperty("connections").GetArrayLength());
    }
}
