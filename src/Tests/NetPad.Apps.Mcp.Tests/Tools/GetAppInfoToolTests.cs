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
        handler.Setup(HttpMethod.Get, "/app/info", HttpStatusCode.OK,
            new AppInfoDto
            {
                Identifier = new AppIdentifierDto { Name = "NetPad", Version = "1.0.0", ProductVersion = "1.0.0-beta" },
                DependencyCheckResult =
                    new AppDependencyCheckDto { DotNetRuntimeVersion = "9.0.0", IsSupportedDotNetEfToolInstalled = true },
                AppDataDirectoryPath = "/home/user/.local/share/NetPad",
                OsDescription = "Arch Linux",
                OsArchitecture = "X64"
            });

        var result = await GetAppInfoTool.GetAppInfo(client, CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        Assert.Equal("NetPad", root.GetProperty("Name").GetString());
        Assert.Equal("1.0.0", root.GetProperty("Version").GetString());
        Assert.Equal("1.0.0-beta", root.GetProperty("ProductVersion").GetString());
        Assert.Equal("9.0.0", root.GetProperty("DotNetRuntimeVersion").GetString());
        Assert.True(root.GetProperty("IsSupportedDotNetEfToolInstalled").GetBoolean());
        Assert.Equal("/home/user/.local/share/NetPad", root.GetProperty("AppDataDirectoryPath").GetString());
        Assert.Equal("Arch Linux", root.GetProperty("OsDescription").GetString());
        Assert.Equal("X64", root.GetProperty("OsArchitecture").GetString());
    }

    [Fact]
    public async Task GetAppInfo_CallsTheAggregateEndpointOnce()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/app/info", HttpStatusCode.OK,
            new AppInfoDto
            {
                Identifier = new AppIdentifierDto { Name = "NetPad" },
                DependencyCheckResult = new AppDependencyCheckDto { DotNetRuntimeVersion = "9.0.0" },
                AppDataDirectoryPath = "/data"
            });

        await GetAppInfoTool.GetAppInfo(client, CancellationToken.None);

        Assert.Single(handler.Requests);
        Assert.Contains(handler.Requests, r => r.Url.Contains("/app/info"));
    }
}
