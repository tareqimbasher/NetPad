using System.Net;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class UpdateScriptCodeToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task UpdateScriptCode_ValidId_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{id}/code", HttpStatusCode.OK);

        var result = await UpdateScriptCodeTool.UpdateScriptCode(
            client, id.ToString(), "var x = 42;", CancellationToken.None);

        Assert.Equal("Script code updated successfully.", result);
    }

    [Fact]
    public async Task UpdateScriptCode_InvalidId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await UpdateScriptCodeTool.UpdateScriptCode(
            client, "invalid", "var x = 1;", CancellationToken.None);

        Assert.Equal("Invalid scriptId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task UpdateScriptCode_SendsCodeInRequestBody()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{id}/code", HttpStatusCode.OK);

        await UpdateScriptCodeTool.UpdateScriptCode(
            client, id.ToString(), "Console.WriteLine(\"hello\");", CancellationToken.None);

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, recorded.Method);
        Assert.Contains("Console.WriteLine", recorded.Body);
    }
}
