using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class SearchPackagesToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task SearchPackages_ReturnsResults()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/packages/search", HttpStatusCode.OK, new PackageSearchResultsDto
        {
            Packages =
            [
                new PackageMetadataDto { PackageId = "Newtonsoft.Json", Version = "13.0.3", Title = "Json.NET" }
            ]
        });

        var result = await SearchPackagesTool.SearchPackages(client, "json");

        var root = JsonDocument.Parse(result).RootElement;
        var packages = root.GetProperty("packages");
        Assert.Equal(1, packages.GetArrayLength());
        Assert.Equal("Newtonsoft.Json", packages[0].GetProperty("packageId").GetString());
        Assert.False(root.GetProperty("hasMorePages").GetBoolean());
    }

    [Fact]
    public async Task SearchPackages_ReportsMorePagesAndUnavailableSources()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/packages/search", HttpStatusCode.OK, new PackageSearchResultsDto
        {
            HasMorePages = true,
            UnavailableSources = ["Broken Local"]
        });

        var result = await SearchPackagesTool.SearchPackages(client, "json");

        var root = JsonDocument.Parse(result).RootElement;
        Assert.True(root.GetProperty("hasMorePages").GetBoolean());
        Assert.Equal("Broken Local", root.GetProperty("unavailableSources")[0].GetString());
    }

    [Fact]
    public async Task SearchPackages_DefaultPagination_UsesCorrectParams()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/packages/search", HttpStatusCode.OK, new PackageSearchResultsDto());

        await SearchPackagesTool.SearchPackages(client, "test");

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("term=test", recorded.Url);
        Assert.Contains("skip=0", recorded.Url);
        Assert.Contains("take=30", recorded.Url);
    }

    [Fact]
    public async Task SearchPackages_CustomPagination_PassesParams()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/packages/search", HttpStatusCode.OK, new PackageSearchResultsDto());

        await SearchPackagesTool.SearchPackages(client, "test", skip: 20, take: 10);

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("skip=20", recorded.Url);
        Assert.Contains("take=10", recorded.Url);
    }
}
