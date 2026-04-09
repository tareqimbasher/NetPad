using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NetPad.Apps.Mcp.Dtos;

namespace NetPad.Apps.Mcp;

/// <summary>
/// HTTP client for the NetPad backend API.
/// </summary>
public class NetPadApiClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public NetPadApiClient(NetPadConnection connection)
        : this(connection, new HttpClientHandler())
    {
    }

    internal NetPadApiClient(NetPadConnection connection, HttpMessageHandler handler)
    {
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(connection.Url),
            // No client-side timeout, rely on server-side timeouts and CancellationToken
            // to avoid premature cancellation of long-running operations like script execution
            Timeout = Timeout.InfiniteTimeSpan
        };
        _httpClient.DefaultRequestHeaders.Add("X-NetPad-Token", connection.Token);
    }

    // --- App ---

    public async Task<AppIdentifierDto> GetAppIdentifierAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<AppIdentifierDto>("/app/identifier", cancellationToken)
               ?? throw new InvalidOperationException("Failed to get app identifier");
    }

    public async Task<AppDependencyCheckDto> CheckDependenciesAsync(CancellationToken cancellationToken = default)
    {
        return await PatchAsync<AppDependencyCheckDto>("/app/check-dependencies", cancellationToken: cancellationToken);
    }

    // --- Scripts ---

    public async Task<ScriptInfoDto[]> GetScriptsInfoAsync(
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var url = "/scripts/info";
        if (!string.IsNullOrWhiteSpace(name))
        {
            url += $"?name={Uri.EscapeDataString(name)}";
        }

        return await GetAsync<ScriptInfoDto[]>(url, cancellationToken) ?? [];
    }

    public async Task<ScriptDto> GetScriptAsync(Guid scriptId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ScriptDto>($"/scripts/{scriptId}", cancellationToken)
               ?? throw new InvalidOperationException($"Script not found: {scriptId}");
    }

    public async Task<string> GetScriptCodeAsync(Guid scriptId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"/scripts/{scriptId}/code",
            cancellationToken: cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<ScriptDto> CreateScriptAsync(CreateScriptDto dto, CancellationToken cancellationToken = default)
    {
        return await PatchAsync<ScriptDto>("/scripts/create", SerializeContent(dto), cancellationToken);
    }

    public async Task<ScriptDto> DuplicateScriptAsync(Guid scriptId, CancellationToken cancellationToken = default)
    {
        return await PatchAsync<ScriptDto>($"/scripts/{scriptId}/duplicate", cancellationToken: cancellationToken);
    }

    public async Task<bool> SaveScriptAsync(Guid scriptId, CancellationToken cancellationToken = default)
    {
        return await PatchAsync<bool>($"/scripts/{scriptId}/save", cancellationToken: cancellationToken);
    }

    public async Task RenameScriptAsync(Guid scriptId, string newName, CancellationToken cancellationToken = default)
    {
        await PatchAsync($"/scripts/{scriptId}/rename", SerializeContent(newName), cancellationToken);
    }

    public async Task UpdateScriptCodeAsync(Guid scriptId, string code, CancellationToken cancellationToken = default)
    {
        await SendAsync(HttpMethod.Put, $"/scripts/{scriptId}/code?externallyInitiated=true", SerializeContent(code),
            cancellationToken);
    }

    public async Task SetScriptKindAsync(Guid scriptId, string kind, CancellationToken cancellationToken = default)
    {
        await SendAsync(HttpMethod.Put, $"/scripts/{scriptId}/kind", SerializeContent(kind), cancellationToken);
    }

    public async Task SetScriptTargetFrameworkAsync(Guid scriptId, string targetFrameworkVersion,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(HttpMethod.Put, $"/scripts/{scriptId}/target-framework-version",
            SerializeContent(targetFrameworkVersion), cancellationToken);
    }

    public async Task SetScriptOptimizationLevelAsync(Guid scriptId, string optimizationLevel,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(HttpMethod.Put, $"/scripts/{scriptId}/optimization-level",
            SerializeContent(optimizationLevel), cancellationToken);
    }

    public async Task SetScriptUseAspNetAsync(Guid scriptId, bool useAspNet,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(HttpMethod.Put, $"/scripts/{scriptId}/use-asp-net", SerializeContent(useAspNet),
            cancellationToken);
    }

    public async Task UpdateScriptNamespacesAsync(Guid scriptId, string[] namespaces,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(HttpMethod.Put, $"/scripts/{scriptId}/namespaces", SerializeContent(namespaces),
            cancellationToken);
    }

    public async Task SetScriptDataConnectionAsync(
        Guid scriptId,
        Guid? dataConnectionId,
        CancellationToken cancellationToken = default)
    {
        var url = $"/scripts/{scriptId}/data-connection";
        if (dataConnectionId.HasValue)
        {
            url += $"?dataConnectionId={dataConnectionId.Value}";
        }

        await SendAsync(HttpMethod.Put, url, cancellationToken: cancellationToken);
    }

    // --- Session ---

    public async Task<Guid?> GetActiveScriptIdAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<Guid?>("/session/active", cancellationToken);
    }

    public async Task<ScriptEnvironmentDto> OpenScriptByIdAsync(Guid scriptId,
        CancellationToken cancellationToken = default)
    {
        return await PatchAsync<ScriptEnvironmentDto>($"/session/open/{scriptId}",
            cancellationToken: cancellationToken);
    }

    public async Task<ScriptEnvironmentDto> OpenScriptByPathAsync(string scriptPath,
        CancellationToken cancellationToken = default)
    {
        return await PatchAsync<ScriptEnvironmentDto>("/session/open/path", SerializeContent(scriptPath),
            cancellationToken);
    }

    public async Task<ScriptEnvironmentDto> GetEnvironmentAsync(Guid scriptId,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<ScriptEnvironmentDto>($"/session/environments/{scriptId}", cancellationToken)
               ?? throw new InvalidOperationException($"Script environment not found: {scriptId}");
    }

    public async Task<ScriptStatusDto> GetEnvironmentStatusAsync(Guid scriptId,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<ScriptStatusDto>($"/session/environments/{scriptId}/status", cancellationToken)
               ?? throw new InvalidOperationException($"Script environment not found: {scriptId}");
    }

    // --- Execution ---

    public async Task<HeadlessRunResult> RunCodeAsync(HeadlessRunRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<HeadlessRunResult>("/headless/run", request, cancellationToken);
    }

    public async Task<HeadlessRunResult> RunScriptAsync(Guid scriptId, int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"/headless/run/{scriptId}";
        if (timeoutMs.HasValue)
        {
            url += $"?timeoutMs={timeoutMs.Value}";
        }

        return await PostAsync<HeadlessRunResult>(url, cancellationToken: cancellationToken);
    }

    public async Task<HeadlessRunResult> RunScriptInGuiAsync(Guid scriptId, int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"/headless/run/{scriptId}/gui";
        if (timeoutMs.HasValue)
        {
            url += $"?timeoutMs={timeoutMs.Value}";
        }

        return await PostAsync<HeadlessRunResult>(url, cancellationToken: cancellationToken);
    }

    public async Task StopScriptAsync(Guid scriptId, CancellationToken cancellationToken = default)
    {
        await PatchAsync($"/scripts/{scriptId}/stop", cancellationToken: cancellationToken);
    }

    // --- Data Connections ---

    public async Task<GetAllConnectionsResponse> GetAllConnectionsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<GetAllConnectionsResponse>("/data-connections", cancellationToken)
               ?? new GetAllConnectionsResponse();
    }

    public async Task<DatabaseStructureDto> GetDatabaseStructureAsync(Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        return await PatchAsync<DatabaseStructureDto>($"/data-connections/{connectionId}/database-structure",
            cancellationToken: cancellationToken);
    }

    public async Task<DataConnectionTestResultDto> TestDataConnectionAsync(Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        return await PatchAsync<DataConnectionTestResultDto>($"/data-connections/{connectionId}/test",
            cancellationToken: cancellationToken);
    }

    public async Task<string[]> GetDatabasesAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        return await PatchAsync<string[]>($"/data-connections/{connectionId}/databases",
            cancellationToken: cancellationToken);
    }

    // --- Packages ---

    public async Task<PackageMetadataDto[]> SearchPackagesAsync(string term, int skip = 0, int take = 30,
        CancellationToken cancellationToken = default)
    {
        var url = $"/packages/search?term={Uri.EscapeDataString(term)}&skip={skip}&take={take}";
        return await GetAsync<PackageMetadataDto[]>(url, cancellationToken) ?? [];
    }

    public async Task<string[]> GetPackageVersionsAsync(string packageId, bool includePrerelease = false,
        CancellationToken cancellationToken = default)
    {
        var url = $"/packages/versions?packageId={Uri.EscapeDataString(packageId)}&includePrerelease={includePrerelease.ToString().ToLowerInvariant()}";
        return await GetAsync<string[]>(url, cancellationToken) ?? [];
    }

    // --- HTTP helpers ---

    private async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, url, cancellationToken: cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
    }

    private async Task<T> PostAsync<T>(string url, object? body = null, CancellationToken cancellationToken = default)
    {
        var content = body != null ? SerializeContent(body) : null;
        using var response = await SendAsync(HttpMethod.Post, url, content, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken)
               ?? throw new InvalidOperationException($"Unexpected null response from POST {url}");
    }

    private async Task<T> PatchAsync<T>(string url, HttpContent? content = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Patch, url, content, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken)
               ?? throw new InvalidOperationException($"Unexpected null response from PATCH {url}");
    }

    private async Task PatchAsync(string url, HttpContent? content = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Patch, url, content, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, url);

        if (content != null)
        {
            request.Content = content;
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            using (response)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"NetPad API error: {(int)response.StatusCode} {response.StatusCode} for {method} {url}. {errorBody}",
                    null,
                    response.StatusCode);
            }
        }

        return response;
    }

    private static StringContent SerializeContent(object value)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
