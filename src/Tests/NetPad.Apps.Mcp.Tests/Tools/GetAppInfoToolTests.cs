using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class GetAppInfoToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task GetAppInfo_ReturnsCompositeInfo()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/app/identifier", HttpStatusCode.OK,
            new AppIdentifierDto { Name = "NetPad", Version = "1.0.0", ProductVersion = "1.0.0-beta" });
        handler.Setup(HttpMethod.Patch, "/app/check-dependencies", HttpStatusCode.OK,
            new AppDependencyCheckDto { DotNetRuntimeVersion = "9.0.0", IsSupportedDotNetEfToolInstalled = true });

        var result = await GetAppInfoTool.GetAppInfo(client, CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        Assert.Equal("NetPad", root.GetProperty("Name").GetString());
        Assert.Equal("1.0.0", root.GetProperty("Version").GetString());
        Assert.Equal("1.0.0-beta", root.GetProperty("ProductVersion").GetString());
        Assert.Equal("9.0.0", root.GetProperty("DotNetRuntimeVersion").GetString());
        Assert.True(root.GetProperty("IsSupportedDotNetEfToolInstalled").GetBoolean());
    }

    [Fact]
    public async Task GetAppInfo_CallsBothEndpoints()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/app/identifier", HttpStatusCode.OK,
            new AppIdentifierDto { Name = "NetPad" });
        handler.Setup(HttpMethod.Patch, "/app/check-dependencies", HttpStatusCode.OK,
            new AppDependencyCheckDto { DotNetRuntimeVersion = "9.0.0" });

        await GetAppInfoTool.GetAppInfo(client, CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains(handler.Requests, r => r.Url.Contains("/app/identifier"));
        Assert.Contains(handler.Requests, r => r.Url.Contains("/app/check-dependencies"));
    }
}
