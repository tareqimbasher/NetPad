using System.Net;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class GetScriptCodeToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task GetScriptCode_ValidId_ReturnsCode()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.SetupRaw(HttpMethod.Get, $"/scripts/{id}/code", HttpStatusCode.OK,
            "Console.WriteLine(42);");

        var result = await GetScriptCodeTool.GetScriptCode(client, id.ToString(), CancellationToken.None);

        Assert.Equal("Console.WriteLine(42);", result);
    }

    [Fact]
    public async Task GetScriptCode_InvalidId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await GetScriptCodeTool.GetScriptCode(client, "bad-id", CancellationToken.None);

        Assert.Equal("Invalid scriptId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task GetScriptCode_CallsCorrectEndpoint()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.SetupRaw(HttpMethod.Get, $"/scripts/{id}/code", HttpStatusCode.OK, "var x = 1;");

        await GetScriptCodeTool.GetScriptCode(client, id.ToString(), CancellationToken.None);

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, recorded.Method);
        Assert.Contains($"/scripts/{id}/code", recorded.Url);
    }
}
