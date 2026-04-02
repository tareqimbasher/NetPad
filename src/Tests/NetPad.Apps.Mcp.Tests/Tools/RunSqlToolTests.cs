using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class RunSqlToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task RunSql_SetsKindToSqlAndParsesConnectionId()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        handler.Setup(HttpMethod.Post, "/api/headless/run", HttpStatusCode.OK,
            new HeadlessRunResult { Status = "completed", Success = true, DurationMs = 10 });

        await RunSqlTool.RunSql(client, "SELECT 1", connId.ToString());

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("\"kind\":\"sql\"", recorded.Body);
        Assert.Contains(connId.ToString(), recorded.Body);
    }

    [Fact]
    public async Task RunSql_WithTimeout_IncludesTimeout()
    {
        var (client, handler) = CreateClient();
        var connId = Guid.NewGuid();
        handler.Setup(HttpMethod.Post, "/api/headless/run", HttpStatusCode.OK,
            new HeadlessRunResult { Status = "completed", Success = true });

        await RunSqlTool.RunSql(client, "SELECT 1", connId.ToString(), timeoutMs: 15000);

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("\"timeoutMs\":15000", recorded.Body);
    }
}
