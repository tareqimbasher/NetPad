using System.Net;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class GetIlToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task GetIl_ValidId_ReturnsIl()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        var ilText = ".method public static void Main() { ret }";
        handler.SetupRaw(HttpMethod.Get, $"/code/{id}/il", HttpStatusCode.OK, ilText);

        var result = await GetIlTool.GetIl(client, id.ToString(), CancellationToken.None);

        Assert.Equal(ilText, result);
    }

    [Fact]
    public async Task GetIl_EmptyResponse_ReturnsNoCodeMessage()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.SetupRaw(HttpMethod.Get, $"/code/{id}/il", HttpStatusCode.OK, "");

        var result = await GetIlTool.GetIl(client, id.ToString(), CancellationToken.None);

        Assert.Equal("Script has no code or produced no IL.", result);
    }

    [Fact]
    public async Task GetIl_InvalidId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await GetIlTool.GetIl(client, "not-valid", CancellationToken.None);

        Assert.Equal("Invalid scriptId format. Expected a GUID.", result);
    }
}
