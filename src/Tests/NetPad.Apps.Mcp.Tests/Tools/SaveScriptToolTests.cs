using System.Net;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class SaveScriptToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task SaveScript_Saved_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.SetupRaw(HttpMethod.Patch, $"/scripts/{id}/save", HttpStatusCode.OK, "true");

        var result = await SaveScriptTool.SaveScript(client, id.ToString(), CancellationToken.None);

        Assert.Equal("Script saved successfully.", result);
    }

    [Fact]
    public async Task SaveScript_UserCancelled_ReturnsCancelledMessage()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.SetupRaw(HttpMethod.Patch, $"/scripts/{id}/save", HttpStatusCode.OK, "false");

        var result = await SaveScriptTool.SaveScript(client, id.ToString(), CancellationToken.None);

        Assert.Equal("Script was not saved (user may have cancelled the save dialog).", result);
    }

    [Fact]
    public async Task SaveScript_InvalidId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await SaveScriptTool.SaveScript(client, "nope", CancellationToken.None);

        Assert.Equal("Invalid scriptId format. Expected a GUID.", result);
    }
}
