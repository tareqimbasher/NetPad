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
            // No client-side timeout — rely on server-side timeouts and CancellationToken
            // to avoid premature cancellation of long-running operations like script execution
            Timeout = System.Threading.Timeout.InfiniteTimeSpan
        };
        _httpClient.DefaultRequestHeaders.Add("X-NetPad-Token", connection.Token);
    }

    // --- Execution ---

    public async Task<HeadlessRunResult> RunCodeAsync(HeadlessRunRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<HeadlessRunResult>("/api/headless/run", request, cancellationToken);
    }

    public async Task<HeadlessRunResult> RunScriptAsync(Guid scriptId, int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        var url = $"/api/headless/run/{scriptId}";
        if (timeoutMs.HasValue)
        {
            url += $"?timeoutMs={timeoutMs.Value}";
        }

        return await PostAsync<HeadlessRunResult>(url, cancellationToken: cancellationToken);
    }

    // --- Session ---

    public async Task<Guid?> GetActiveScriptIdAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<Guid?>("/session/active", cancellationToken);
    }

    public async Task<ScriptEnvironmentDto[]> GetEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<ScriptEnvironmentDto[]>("/session/environments", cancellationToken) ?? [];
    }

    public async Task<ScriptEnvironmentDto> GetEnvironmentAsync(Guid scriptId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ScriptEnvironmentDto>($"/session/environments/{scriptId}", cancellationToken)
               ?? throw new InvalidOperationException($"Script environment not found: {scriptId}");
    }

    public async Task<ScriptStatusDto> GetEnvironmentStatusAsync(Guid scriptId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ScriptStatusDto>($"/session/environments/{scriptId}/status", cancellationToken)
               ?? throw new InvalidOperationException($"Script environment not found: {scriptId}");
    }

    // --- Scripts ---

    public async Task<ScriptSummaryDto[]> GetAllScriptsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<ScriptSummaryDto[]>("/scripts", cancellationToken) ?? [];
    }

    public async Task<ScriptSummaryDto[]> FindScriptsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ScriptSummaryDto[]>($"/scripts/find?name={Uri.EscapeDataString(name)}", cancellationToken) ?? [];
    }

    public async Task<string> GetScriptCodeAsync(Guid scriptId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"/scripts/{scriptId}/code", cancellationToken: cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<ScriptDto> CreateScriptAsync(CreateScriptDto dto, CancellationToken cancellationToken = default)
    {
        return await PatchAsync<ScriptDto>("/scripts/create", SerializeContent(dto), cancellationToken);
    }

    public async Task UpdateScriptCodeAsync(Guid scriptId, string code, CancellationToken cancellationToken = default)
    {
        await SendAsync(HttpMethod.Put, $"/scripts/{scriptId}/code", SerializeContent(code), cancellationToken);
    }

    public async Task<bool> SaveScriptAsync(Guid scriptId, CancellationToken cancellationToken = default)
    {
        return await PatchAsync<bool>($"/scripts/{scriptId}/save", cancellationToken: cancellationToken);
    }

    public async Task RenameScriptAsync(Guid scriptId, string newName, CancellationToken cancellationToken = default)
    {
        await PatchAsync($"/scripts/{scriptId}/rename", SerializeContent(newName), cancellationToken);
    }

    public async Task SetScriptDataConnectionAsync(Guid scriptId, Guid? dataConnectionId, CancellationToken cancellationToken = default)
    {
        var url = $"/scripts/{scriptId}/data-connection";
        if (dataConnectionId.HasValue)
        {
            url += $"?dataConnectionId={dataConnectionId.Value}";
        }

        await SendAsync(HttpMethod.Put, url, cancellationToken: cancellationToken);
    }

    public async Task RunScriptInGuiAsync(Guid scriptId, bool captureOutput = false, CancellationToken cancellationToken = default)
    {
        var url = $"/scripts/{scriptId}/run";
        if (captureOutput)
        {
            url += "?captureOutput=true";
        }

        // RunOptions with no specific code
        await PatchAsync(url, SerializeContent(new { }), cancellationToken);
    }

    public async Task<HeadlessRunResult> GetRunOutputAsync(Guid scriptId, bool wait = true, int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        var url = $"/scripts/{scriptId}/run-output?wait={(wait ? "true" : "false")}";
        if (timeoutMs.HasValue)
        {
            url += $"&timeoutMs={timeoutMs.Value}";
        }

        return await GetAsync<HeadlessRunResult>(url, cancellationToken)
               ?? throw new InvalidOperationException("Failed to get run output");
    }

    // --- Data Connections ---

    public async Task<GetAllConnectionsResponse> GetAllConnectionsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<GetAllConnectionsResponse>("/data-connections", cancellationToken)
               ?? new GetAllConnectionsResponse();
    }

    public async Task<DatabaseStructureDto> GetDatabaseStructureAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        return await PatchAsync<DatabaseStructureDto>($"/data-connections/{connectionId}/database-structure", cancellationToken: cancellationToken);
    }

    // --- Packages ---

    public async Task<PackageMetadataDto[]> SearchPackagesAsync(string term, int skip = 0, int take = 30, CancellationToken cancellationToken = default)
    {
        var url = $"/packages/search?term={Uri.EscapeDataString(term)}&skip={skip}&take={take}";
        return await GetAsync<PackageMetadataDto[]>(url, cancellationToken) ?? [];
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

    private async Task<T> PatchAsync<T>(string url, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Patch, url, content, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken)
               ?? throw new InvalidOperationException($"Unexpected null response from PATCH {url}");
    }

    private async Task PatchAsync(string url, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Patch, url, content, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content = null, CancellationToken cancellationToken = default)
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
