using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class GetDatabasesToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task GetDatabases_ValidId_ReturnsDatabaseList()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/data-connections/{connId}/databases", HttpStatusCode.OK,
            new[] { "master", "tempdb", "myapp" });

        var result = await GetDatabasesTool.GetDatabases(client, connId.ToString(), CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        var arr = doc.RootElement;
        Assert.Equal(3, arr.GetArrayLength());
        Assert.Equal("master", arr[0].GetString());
        Assert.Equal("tempdb", arr[1].GetString());
        Assert.Equal("myapp", arr[2].GetString());
    }

    [Fact]
    public async Task GetDatabases_InvalidId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await GetDatabasesTool.GetDatabases(client, "bad-id", CancellationToken.None);

        Assert.Equal("Invalid connectionId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task GetDatabases_NoDatabases_ReturnsEmptyArray()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/data-connections/{connId}/databases", HttpStatusCode.OK,
            Array.Empty<string>());

        var result = await GetDatabasesTool.GetDatabases(client, connId.ToString(), CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }
}
