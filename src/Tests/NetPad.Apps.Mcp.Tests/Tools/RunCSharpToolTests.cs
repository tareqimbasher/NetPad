using System.Net;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class RunCSharpToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    private static HeadlessRunResult SuccessResult(params string[] output)
    {
        return new HeadlessRunResult
        {
            Status = "completed",
            Success = true,
            DurationMs = 50,
            Output = output.Select(o =>
            {
                var json = JsonSerializer.Serialize(o);
                return JsonDocument.Parse(json).RootElement.Clone();
            }).ToList()
        };
    }

    [Fact]
    public async Task RunCSharp_BasicCode_CallsRunCodeWithCSharpKind()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Post, "/headless/run", HttpStatusCode.OK, SuccessResult("42"));

        await RunCSharpTool.RunCSharp(client, "Console.WriteLine(42);");

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("\"kind\":\"csharp\"", recorded.Body);
        Assert.Contains("Console.WriteLine(42);", recorded.Body);
    }

    [Fact]
    public async Task RunCSharp_WithPackages_DeserializesPackageJson()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Post, "/headless/run", HttpStatusCode.OK, SuccessResult());

        var packages = new[] { new PackageInput { Id = "Newtonsoft.Json", Version = "13.0.3" } };
        await RunCSharpTool.RunCSharp(client, "var x = 1;", packages: packages);

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("\"references\"", recorded.Body);
        Assert.Contains("Newtonsoft.Json", recorded.Body);
        Assert.Contains("13.0.3", recorded.Body);
    }

    [Fact]
    public async Task RunCSharp_WithTimeout_IncludesTimeoutInRequest()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Post, "/headless/run", HttpStatusCode.OK, SuccessResult());

        await RunCSharpTool.RunCSharp(client, "Thread.Sleep(1000);", timeoutMs: 5000);

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("\"timeoutMs\":5000", recorded.Body);
    }

    [Fact]
    public async Task RunCSharp_WithDataConnection_ParsesGuid()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Post, "/headless/run", HttpStatusCode.OK, SuccessResult());

        var connId = Guid.NewGuid();
        await RunCSharpTool.RunCSharp(client, "SELECT 1", dataConnectionId: connId.ToString());

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains(connId.ToString(), recorded.Body);
    }

    [Fact]
    public async Task RunCSharp_WithTargetFramework_IncludesInRequest()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Post, "/headless/run", HttpStatusCode.OK, SuccessResult());

        await RunCSharpTool.RunCSharp(client, "var x = 1;", targetFramework: "DotNet8");

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("DotNet8", recorded.Body);
    }

    [Fact]
    public async Task RunCSharp_InvalidDataConnectionId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await RunCSharpTool.RunCSharp(client, "var x = 1;", dataConnectionId: "not-a-guid");

        Assert.Equal("Invalid dataConnectionId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task RunCSharp_ReturnsFormattedResult()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Post, "/headless/run", HttpStatusCode.OK, SuccessResult("Hello"));

        var result = await RunCSharpTool.RunCSharp(client, "Console.WriteLine(\"Hello\");");

        var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Hello", doc.RootElement.GetProperty("Output")[0].GetProperty("Body").GetString());
    }

    [Fact]
    public async Task RunCSharp_WithAssemblyPaths_IncludesAssemblyReferences()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Post, "/headless/run", HttpStatusCode.OK, SuccessResult());

        await RunCSharpTool.RunCSharp(client, "var x = 1;",
            assemblyPaths: ["/path/to/MyLib.dll"]);

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("AssemblyFileReference", recorded.Body);
        Assert.Contains("/path/to/MyLib.dll", recorded.Body);
    }

    [Fact]
    public async Task RunCSharp_WithPackagesNoVersion_ResolvesLatestVersion()
    {
        var (client, handler) = CreateClient();
        handler.SetupRaw(HttpMethod.Get, "/packages/versions", HttpStatusCode.OK,
            "[\"12.0.0\",\"13.0.3\"]");
        handler.Setup(HttpMethod.Post, "/headless/run", HttpStatusCode.OK, SuccessResult());

        var packages = new[] { new PackageInput { Id = "Newtonsoft.Json" } };
        await RunCSharpTool.RunCSharp(client, "var x = 1;", packages: packages);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("/packages/versions", handler.Requests[0].Url);
        Assert.Contains("13.0.3", handler.Requests[1].Body);
    }

    [Fact]
    public async Task RunCSharp_WithEmptyPackageId_ReturnsValidationError()
    {
        var (client, _) = CreateClient();

        var packages = new[] { new PackageInput { Id = " " } };
        var result = await RunCSharpTool.RunCSharp(client, "var x = 1;", packages: packages);

        Assert.Contains("'id' field", result);
    }

    [Fact]
    public async Task RunCSharp_WithUnknownPackage_ReturnsResolutionError()
    {
        var (client, handler) = CreateClient();
        handler.SetupRaw(HttpMethod.Get, "/packages/versions", HttpStatusCode.OK, "[]");

        var packages = new[] { new PackageInput { Id = "NonExistent.Package" } };
        var result = await RunCSharpTool.RunCSharp(client, "var x = 1;", packages: packages);

        Assert.Contains("Could not resolve versions", result);
        Assert.Contains("NonExistent.Package", result);
    }

    [Fact]
    public async Task RunCSharp_WithPackagesAndAssemblyPaths_BuildsCombinedReferences()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Post, "/headless/run", HttpStatusCode.OK, SuccessResult());

        var packages = new[] { new PackageInput { Id = "Dapper", Version = "2.1.0" } };
        await RunCSharpTool.RunCSharp(client, "var x = 1;",
            packages: packages, assemblyPaths: ["/libs/Custom.dll"]);

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("Dapper", recorded.Body);
        Assert.Contains("PackageReference", recorded.Body);
        Assert.Contains("AssemblyFileReference", recorded.Body);
        Assert.Contains("/libs/Custom.dll", recorded.Body);
    }
}
