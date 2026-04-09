using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class GetDatabaseStructureToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task GetDatabaseStructure_ValidId_ReturnsStructure()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/data-connections/{connId}/database-structure", HttpStatusCode.OK,
            new DatabaseStructureDto
            {
                DatabaseName = "TestDB",
                Schemas = [new DatabaseSchemaDto { Name = "dbo", Tables = [] }]
            });

        var result = await GetDatabaseStructureTool.GetDatabaseStructure(client, connId.ToString(), CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        Assert.Equal("TestDB", doc.RootElement.GetProperty("databaseName").GetString());
    }

    [Fact]
    public async Task GetDatabaseStructure_InvalidId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await GetDatabaseStructureTool.GetDatabaseStructure(client, "not-a-guid", CancellationToken.None);

        Assert.Equal("Invalid connectionId format. Expected a GUID.", result);
    }
}
