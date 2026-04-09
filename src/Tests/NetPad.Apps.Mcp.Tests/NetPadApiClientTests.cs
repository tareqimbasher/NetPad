using System.Net;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;

namespace NetPad.Apps.Mcp.Tests;

public class NetPadApiClientTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    [Fact]
    public async Task AllRequests_IncludeAuthTokenHeader()
    {
        var (client, handler) = CreateClient();
        handler.SetupRaw(HttpMethod.Get, "/session/active", HttpStatusCode.OK, "null");

        await client.GetActiveScriptIdAsync();

        var request = Assert.Single(handler.Requests);
        Assert.True(request.Headers.ContainsKey("X-NetPad-Token"));
        Assert.Equal("test-token", request.Headers["X-NetPad-Token"][0]);
    }

    // --- Execution ---

    [Fact]
    public async Task RunCodeAsync_PostsToCorrectUrl()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Post, "/headless/run", HttpStatusCode.OK,
            new HeadlessRunResult { Status = "completed", Success = true });

        var request = new HeadlessRunRequest { Code = "Console.WriteLine(1);" };
        await client.RunCodeAsync(request);

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, recorded.Method);
        Assert.Equal("/headless/run", recorded.Url);
    }

    [Fact]
    public async Task RunCodeAsync_SerializesRequestBody()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Post, "/headless/run", HttpStatusCode.OK,
            new HeadlessRunResult { Status = "completed", Success = true });

        var request = new HeadlessRunRequest { Code = "var x = 1;", Kind = "sql", TimeoutMs = 5000 };
        await client.RunCodeAsync(request);

        var recorded = Assert.Single(handler.Requests);
        Assert.NotNull(recorded.Body);
        Assert.Contains("\"code\":\"var x = 1;\"", recorded.Body);
        Assert.Contains("\"kind\":\"sql\"", recorded.Body);
        Assert.Contains("\"timeoutMs\":5000", recorded.Body);
    }

    [Fact]
    public async Task RunCodeAsync_DeserializesResponse()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Post, "/headless/run", HttpStatusCode.OK,
            new HeadlessRunResult { Status = "completed", Success = true, DurationMs = 123.4 });

        var result = await client.RunCodeAsync(new HeadlessRunRequest { Code = "1" });

        Assert.Equal("completed", result.Status);
        Assert.True(result.Success);
        Assert.Equal(123.4, result.DurationMs);
    }

    [Fact]
    public async Task RunScriptAsync_PostsToCorrectUrlWithoutTimeout()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Post, $"/headless/run/{id}", HttpStatusCode.OK,
            new HeadlessRunResult { Status = "completed", Success = true });

        await client.RunScriptAsync(id);

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal($"/headless/run/{id}", recorded.Url);
    }

    [Fact]
    public async Task RunScriptAsync_IncludesTimeoutInQuery()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Post, $"/headless/run/{id}", HttpStatusCode.OK,
            new HeadlessRunResult { Status = "completed", Success = true });

        await client.RunScriptAsync(id, timeoutMs: 10000);

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("timeoutMs=10000", recorded.Url);
    }

    // --- Session ---

    [Fact]
    public async Task GetActiveScriptIdAsync_GetsCorrectUrl()
    {
        var (client, handler) = CreateClient();
        var expected = Guid.NewGuid();
        handler.SetupRaw(HttpMethod.Get, "/session/active", HttpStatusCode.OK,
            $"\"{expected}\"");

        var result = await client.GetActiveScriptIdAsync();

        Assert.Equal(expected, result);
        var recorded = Assert.Single(handler.Requests);
        Assert.Equal("/session/active", recorded.Url);
    }

    [Fact]
    public async Task GetEnvironmentStatusAsync_GetsCorrectUrl()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, $"/session/environments/{id}/status", HttpStatusCode.OK,
            new ScriptStatusDto { ScriptId = id, Name = "Test", Status = "Running" });

        var result = await client.GetEnvironmentStatusAsync(id);

        Assert.Equal("Running", result.Status);
    }

    // --- Scripts ---

    [Fact]
    public async Task GetScriptsInfoAsync_GetsCorrectUrlWithoutFilter()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/scripts/info", HttpStatusCode.OK, Array.Empty<ScriptInfoDto>());

        await client.GetScriptsInfoAsync();

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal("/scripts/info", recorded.Url);
    }

    [Fact]
    public async Task GetScriptsInfoAsync_EncodesNameInUrl()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/scripts/info", HttpStatusCode.OK, Array.Empty<ScriptInfoDto>());

        await client.GetScriptsInfoAsync("my script & more");

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("name=my%20script%20%26%20more", recorded.Url);
    }

    [Fact]
    public async Task GetScriptCodeAsync_ReturnsRawContent()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.SetupRaw(HttpMethod.Get, $"/scripts/{id}/code", HttpStatusCode.OK,
            "Console.WriteLine(42);");

        var code = await client.GetScriptCodeAsync(id);

        Assert.Equal("Console.WriteLine(42);", code);
    }

    [Fact]
    public async Task CreateScriptAsync_PatchesToCorrectUrl()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Patch, "/scripts/create", HttpStatusCode.OK,
            new ScriptDto { Id = Guid.NewGuid(), Name = "New Script" });

        var dto = new CreateScriptDto { Name = "New Script", Code = "var x = 1;" };
        var result = await client.CreateScriptAsync(dto);

        Assert.Equal("New Script", result.Name);
        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, recorded.Method);
    }

    [Fact]
    public async Task SetScriptDataConnectionAsync_IncludesConnectionIdInQuery()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        var connId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, "/scripts/", HttpStatusCode.OK);

        await client.SetScriptDataConnectionAsync(scriptId, connId);

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains($"dataConnectionId={connId}", recorded.Url);
    }

    [Fact]
    public async Task SetScriptDataConnectionAsync_NoConnectionId_OmitsQueryParam()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Put, "/scripts/", HttpStatusCode.OK);

        await client.SetScriptDataConnectionAsync(scriptId, null);

        var recorded = Assert.Single(handler.Requests);
        Assert.DoesNotContain("dataConnectionId", recorded.Url);
    }

    [Fact]
    public async Task DuplicateScriptAsync_PatchesToCorrectUrl()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/scripts/{id}/duplicate", HttpStatusCode.OK,
            new ScriptDto { Id = Guid.NewGuid(), Name = "Script Copy" });

        var result = await client.DuplicateScriptAsync(id);

        Assert.Equal("Script Copy", result.Name);
        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, recorded.Method);
        Assert.Contains($"/scripts/{id}/duplicate", recorded.Url);
    }

    // --- Data Connections ---

    [Fact]
    public async Task TestDataConnectionAsync_PatchesToCorrectUrl()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/data-connections/{id}/test", HttpStatusCode.OK,
            new DataConnectionTestResultDto { Success = true, Message = "Connection successful" });

        var result = await client.TestDataConnectionAsync(id);

        Assert.True(result.Success);
        Assert.Equal("Connection successful", result.Message);
        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, recorded.Method);
        Assert.Contains($"/data-connections/{id}/test", recorded.Url);
    }

    [Fact]
    public async Task TestDataConnectionAsync_DeserializesFailureResult()
    {
        var (client, handler) = CreateClient();
        var id = Guid.NewGuid();
        handler.Setup(HttpMethod.Patch, $"/data-connections/{id}/test", HttpStatusCode.OK,
            new DataConnectionTestResultDto { Success = false, Message = "Connection refused" });

        var result = await client.TestDataConnectionAsync(id);

        Assert.False(result.Success);
        Assert.Equal("Connection refused", result.Message);
    }

    // --- Packages ---

    [Fact]
    public async Task SearchPackagesAsync_IncludesPaginationParams()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/packages/search", HttpStatusCode.OK, Array.Empty<PackageMetadataDto>());

        await client.SearchPackagesAsync("json", skip: 10, take: 5);

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("term=json", recorded.Url);
        Assert.Contains("skip=10", recorded.Url);
        Assert.Contains("take=5", recorded.Url);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_GetsCorrectUrlWithDefaults()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/packages/versions", HttpStatusCode.OK,
            new[] { "1.0.0", "2.0.0" });

        var result = await client.GetPackageVersionsAsync("Newtonsoft.Json");

        Assert.Equal(2, result.Length);
        Assert.Equal("1.0.0", result[0]);
        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("packageId=Newtonsoft.Json", recorded.Url);
        Assert.Contains("includePrerelease=false", recorded.Url);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_IncludesPrerelease()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/packages/versions", HttpStatusCode.OK,
            new[] { "1.0.0-beta", "1.0.0" });

        await client.GetPackageVersionsAsync("MyPkg", includePrerelease: true);

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("includePrerelease=true", recorded.Url);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_EncodesPackageId()
    {
        var (client, handler) = CreateClient();
        handler.Setup(HttpMethod.Get, "/packages/versions", HttpStatusCode.OK, Array.Empty<string>());

        await client.GetPackageVersionsAsync("My Package");

        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("packageId=My%20Package", recorded.Url);
    }

    // --- Error Handling ---

    [Fact]
    public async Task NonSuccessStatusCode_ThrowsHttpRequestException()
    {
        var (client, handler) = CreateClient();
        handler.SetupRaw(HttpMethod.Get, "/session/active", HttpStatusCode.InternalServerError,
            "Something went wrong");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetActiveScriptIdAsync());

        Assert.Contains("500", ex.Message);
        Assert.Contains("Something went wrong", ex.Message);
    }

    [Fact]
    public async Task NotFoundStatusCode_ThrowsWithUrlInfo()
    {
        var (client, handler) = CreateClient();
        handler.SetupRaw(HttpMethod.Get, "/scripts/info", HttpStatusCode.NotFound, "Not found");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetScriptsInfoAsync());

        Assert.Contains("404", ex.Message);
    }
}
