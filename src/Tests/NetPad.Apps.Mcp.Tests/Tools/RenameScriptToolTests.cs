using System.Net;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class RenameScriptToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task RenameScript_ValidId_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/scripts/{id}/rename", HttpStatusCode.OK);

        var result = await RenameScriptTool.RenameScript(client, id.ToString(), "New Name", CancellationToken.None);

        Assert.Equal("Script renamed to 'New Name'.", result);
    }

    [Fact]
    public async Task RenameScript_InvalidId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await RenameScriptTool.RenameScript(client, "bad", "New Name", CancellationToken.None);

        Assert.Equal("Invalid scriptId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task RenameScript_CallsCorrectEndpointWithNewName()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/scripts/{id}/rename", HttpStatusCode.OK);

        await RenameScriptTool.RenameScript(client, id.ToString(), "Renamed Script", CancellationToken.None);

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, recorded.Method);
        Assert.Contains($"/scripts/{id}/rename", recorded.Url);
        Assert.Contains("Renamed Script", recorded.Body);
    }
}
