using System.Net;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class UpdateScriptToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task UpdateScript_CodeOnly_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/code", HttpStatusCode.NoContent);

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), code: "Console.WriteLine(42);");

        Assert.Equal("Script updated: code.", result);
    }

    [Fact]
    public async Task UpdateScript_CodeOnly_SendsPutToCorrectUrl()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/code", HttpStatusCode.NoContent);

        await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), code: "Console.WriteLine(42);");

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, recorded.Method);
        Assert.Contains($"/scripts/{scriptId}/code", recorded.Url);
    }

    [Fact]
    public async Task UpdateScript_KindProgram_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/kind", HttpStatusCode.NoContent);

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), kind: "Program");

        Assert.Equal("Script updated: kind (Program).", result);
    }

    [Fact]
    public async Task UpdateScript_KindSql_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/kind", HttpStatusCode.NoContent);

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), kind: "SQL");

        Assert.Equal("Script updated: kind (SQL).", result);
    }

    [Fact]
    public async Task UpdateScript_InvalidKind_ReturnsValidationError()
    {
        var (client, _) = CreateClient();
        var scriptId = Guid.NewGuid();

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), kind: "Expression");

        Assert.Contains("Invalid kind", result);
        Assert.Contains("Program", result);
        Assert.Contains("SQL", result);
    }

    [Fact]
    public async Task UpdateScript_KindCaseInsensitive_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/kind", HttpStatusCode.NoContent);

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), kind: "program");

        Assert.Contains("Script updated:", result);
    }

    [Fact]
    public async Task UpdateScript_TargetFramework_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/target-framework-version", HttpStatusCode.NoContent);

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), targetFrameworkVersion: "DotNet9");

        Assert.Equal("Script updated: target framework (DotNet9).", result);
    }

    [Fact]
    public async Task UpdateScript_OptimizationLevelDebug_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/optimization-level", HttpStatusCode.NoContent);

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), optimizationLevel: "Debug");

        Assert.Equal("Script updated: optimization level (Debug).", result);
    }

    [Fact]
    public async Task UpdateScript_OptimizationLevelRelease_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/optimization-level", HttpStatusCode.NoContent);

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), optimizationLevel: "Release");

        Assert.Equal("Script updated: optimization level (Release).", result);
    }

    [Fact]
    public async Task UpdateScript_InvalidOptimizationLevel_ReturnsValidationError()
    {
        var (client, _) = CreateClient();
        var scriptId = Guid.NewGuid();

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), optimizationLevel: "Fast");

        Assert.Contains("Invalid optimizationLevel", result);
        Assert.Contains("Debug", result);
        Assert.Contains("Release", result);
    }

    [Fact]
    public async Task UpdateScript_UseAspNetTrue_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/use-asp-net", HttpStatusCode.NoContent);

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), useAspNet: true);

        Assert.Equal("Script updated: ASP.NET (enabled).", result);
    }

    [Fact]
    public async Task UpdateScript_UseAspNetFalse_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/use-asp-net", HttpStatusCode.NoContent);

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), useAspNet: false);

        Assert.Equal("Script updated: ASP.NET (disabled).", result);
    }

    [Fact]
    public async Task UpdateScript_Namespaces_ReturnsSuccessMessage()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/namespaces", HttpStatusCode.NoContent);

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), namespaces: ["System", "System.Linq"]);

        Assert.Equal("Script updated: namespaces.", result);
    }

    [Fact]
    public async Task UpdateScript_NamespacesWithUsingPrefix_ReturnsValidationError()
    {
        var (client, _) = CreateClient();
        var scriptId = Guid.NewGuid();

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), namespaces: ["using System.Linq"]);

        Assert.Contains("Invalid namespace", result);
        Assert.Contains("using System.Linq", result);
    }

    [Fact]
    public async Task UpdateScript_NamespacesWithSemicolon_ReturnsValidationError()
    {
        var (client, _) = CreateClient();
        var scriptId = Guid.NewGuid();

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(), namespaces: ["System.Linq;"]);

        Assert.Contains("Invalid namespace", result);
        Assert.Contains("System.Linq;", result);
    }

    [Fact]
    public async Task UpdateScript_InvalidScriptId_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();

        var result = await UpdateScriptTool.UpdateScript(
            client, "bad-id", code: "test");

        Assert.Equal("Invalid scriptId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task UpdateScript_NoParams_ReturnsErrorMessage()
    {
        var (client, _) = CreateClient();
        var scriptId = Guid.NewGuid();

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString());

        Assert.Contains("No properties to update", result);
    }

    [Fact]
    public async Task UpdateScript_MultipleParams_AllApplied()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/code", HttpStatusCode.NoContent);
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/kind", HttpStatusCode.NoContent);
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/optimization-level", HttpStatusCode.NoContent);

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(),
            code: "Console.WriteLine(42);",
            kind: "Program",
            optimizationLevel: "Release");

        Assert.Contains("code", result);
        Assert.Contains("kind (Program)", result);
        Assert.Contains("optimization level (Release)", result);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task UpdateScript_ValidationFailsBeforeApiCalls_NoRequestsMade()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();

        var result = await UpdateScriptTool.UpdateScript(
            client, scriptId.ToString(),
            code: "Console.WriteLine(42);",
            kind: "InvalidKind");

        Assert.Contains("Invalid kind", result);
        Assert.Empty(handler.Requests);
    }
}
