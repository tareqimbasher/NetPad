using System.Net;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class GetScriptToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task GetScript_ValidId_ReturnsScriptJson()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, $"/scripts/{scriptId}", HttpStatusCode.OK, new ScriptDto
        {
            Id = scriptId,
            Name = "Test Script",
            Code = "Console.WriteLine(42);",
            Config = new ScriptConfigDto
            {
                Kind = "Program",
                TargetFrameworkVersion = "DotNet9",
                OptimizationLevel = "Debug",
                UseAspNet = false,
                Namespaces = ["System", "System.Linq"]
            }
        });

        var result = await GetScriptTool.GetScript(client, scriptId.ToString(), CancellationToken.None);

        Assert.Contains("Test Script", result);
        Assert.Contains("Program", result);
        Assert.Contains("DotNet9", result);
    }

    [Fact]
    public async Task GetScript_InvalidScriptId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await GetScriptTool.GetScript(client, "bad-id", CancellationToken.None);

        Assert.Equal("Invalid scriptId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task GetScript_ValidId_SendsGetToCorrectUrl()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, $"/scripts/{scriptId}", HttpStatusCode.OK, new ScriptDto
        {
            Id = scriptId,
            Name = "Test",
            Code = ""
        });

        await GetScriptTool.GetScript(client, scriptId.ToString(), CancellationToken.None);

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, recorded.Method);
        Assert.Equal($"/scripts/{scriptId}", recorded.Url);
    }
}
