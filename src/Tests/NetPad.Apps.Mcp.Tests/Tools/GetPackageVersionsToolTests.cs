using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class GetPackageVersionsToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task GetPackageVersions_ReturnsVersionList()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/packages/versions", HttpStatusCode.OK,
            new[] { "13.0.3", "13.0.2", "13.0.1" });

        var result = await GetPackageVersionsTool.GetPackageVersions(client, "Newtonsoft.Json");

        var doc = JsonDocument.Parse(result);
        var arr = doc.RootElement;
        Assert.Equal(3, arr.GetArrayLength());
        Assert.Equal("13.0.3", arr[0].GetString());
    }

    [Fact]
    public async Task GetPackageVersions_DefaultParams_DoesNotIncludePrerelease()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/packages/versions", HttpStatusCode.OK, Array.Empty<string>());

        await GetPackageVersionsTool.GetPackageVersions(client, "TestPkg");

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("packageId=TestPkg", recorded.Url);
        Assert.Contains("includePrerelease=false", recorded.Url);
    }

    [Fact]
    public async Task GetPackageVersions_IncludePrerelease_PassesParam()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/packages/versions", HttpStatusCode.OK,
            new[] { "14.0.0-beta1", "13.0.3" });

        await GetPackageVersionsTool.GetPackageVersions(client, "Newtonsoft.Json", includePrerelease: true);

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("includePrerelease=true", recorded.Url);
    }

    [Fact]
    public async Task GetPackageVersions_NoVersions_ReturnsEmptyArray()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/packages/versions", HttpStatusCode.OK, Array.Empty<string>());

        var result = await GetPackageVersionsTool.GetPackageVersions(client, "NonExistentPackage");

        var doc = JsonDocument.Parse(result);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }
}
