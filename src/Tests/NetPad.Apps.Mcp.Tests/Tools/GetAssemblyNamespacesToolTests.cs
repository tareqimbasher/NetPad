using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class GetAssemblyNamespacesToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task GetAssemblyNamespaces_Package_ReturnsNamespaces()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Patch, "/assemblies/namespaces", HttpStatusCode.OK,
            new[] { "Newtonsoft.Json", "Newtonsoft.Json.Linq", "Newtonsoft.Json.Converters" });

        var result = await GetAssemblyNamespacesTool.GetAssemblyNamespaces(
            client, packageId: "Newtonsoft.Json", version: "13.0.3");

        var doc = JsonDocument.Parse(result);
        var arr = doc.RootElement;
        Assert.Equal(3, arr.GetArrayLength());
        Assert.Equal("Newtonsoft.Json", arr[0].GetString());
    }

    [Fact]
    public async Task GetAssemblyNamespaces_Package_SendsDiscriminator()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Patch, "/assemblies/namespaces", HttpStatusCode.OK, Array.Empty<string>());

        await GetAssemblyNamespacesTool.GetAssemblyNamespaces(
            client, packageId: "TestPkg", version: "1.0.0");

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("PackageReference", recorded.Body);
        Assert.Contains("TestPkg", recorded.Body);
        Assert.Contains("1.0.0", recorded.Body);
    }

    [Fact]
    public async Task GetAssemblyNamespaces_Package_MissingVersion_ReturnsError()
    {
        var (client, _) = CreateClient();

        var result = await GetAssemblyNamespacesTool.GetAssemblyNamespaces(
            client, packageId: "Newtonsoft.Json");

        Assert.Equal("version is required when packageId is provided.", result);
    }

    [Fact]
    public async Task GetAssemblyNamespaces_AssemblyPath_ReturnsNamespaces()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Patch, "/assemblies/namespaces", HttpStatusCode.OK,
            new[] { "MyLib", "MyLib.Models" });

        var result = await GetAssemblyNamespacesTool.GetAssemblyNamespaces(
            client, assemblyPath: "/path/to/MyLib.dll");

        var doc = JsonDocument.Parse(result);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetAssemblyNamespaces_AssemblyPath_SendsDiscriminator()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Patch, "/assemblies/namespaces", HttpStatusCode.OK, Array.Empty<string>());

        await GetAssemblyNamespacesTool.GetAssemblyNamespaces(
            client, assemblyPath: "/path/to/MyLib.dll");

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("AssemblyFileReference", recorded.Body);
        Assert.Contains("MyLib.dll", recorded.Body);
    }

    [Fact]
    public async Task GetAssemblyNamespaces_NoParams_ReturnsError()
    {
        var (client, _) = CreateClient();

        var result = await GetAssemblyNamespacesTool.GetAssemblyNamespaces(client);

        Assert.Equal("Either packageId (with version) or assemblyPath must be provided.", result);
    }
}
