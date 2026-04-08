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

        var packages = """[{"id":"Newtonsoft.Json","version":"13.0.3"}]""";
        await RunCSharpTool.RunCSharp(client, "var x = 1;", packages: packages);

        var recorded = Assert.Single(handler.Requests);
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
    public async Task RunCSharp_ReturnsFormattedResult()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Post, "/headless/run", HttpStatusCode.OK, SuccessResult("Hello"));

        var result = await RunCSharpTool.RunCSharp(client, "Console.WriteLine(\"Hello\");");

        var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Hello", doc.RootElement.GetProperty("Output")[0].GetString());
    }
}
