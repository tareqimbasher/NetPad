using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class GetSettingsToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task GetSettings_ReturnsSettingsJson()
    {
        var (client, handler) = CreateClient();
        var settings = new
        {
            scriptsDirectoryPath = "/scripts",
            autoCheckUpdates = true,
            appearance = new { theme = "Dark" }
        };
        handler.Setup(HttpMethod.Get, "/settings", HttpStatusCode.OK, settings);

        var result = await GetSettingsTool.GetSettings(client, CancellationToken.None);

        var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        Assert.Equal("/scripts", root.GetProperty("scriptsDirectoryPath").GetString());
        Assert.True(root.GetProperty("autoCheckUpdates").GetBoolean());
        Assert.Equal("Dark", root.GetProperty("appearance").GetProperty("theme").GetString());
    }

    [Fact]
    public async Task GetSettings_CallsSettingsEndpoint()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/settings", HttpStatusCode.OK, new { });

        await GetSettingsTool.GetSettings(client, CancellationToken.None);

        Assert.Single(handler.Requests);
        Assert.Contains("/settings", handler.Requests[0].Url);
    }
}
