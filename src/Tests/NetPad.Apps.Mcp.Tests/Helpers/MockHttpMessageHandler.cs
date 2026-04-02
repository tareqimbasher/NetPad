using System.Net;
using System.Text;
using System.Text.Json;

namespace NetPad.Apps.Mcp.Tests.Helpers;

internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(HttpMethod Method, string UrlContains, HttpStatusCode Status, string? ResponseJson)> _setups = [];
    private readonly List<RecordedRequest> _requests = [];

    public IReadOnlyList<RecordedRequest> Requests => _requests;

    public void Setup(HttpMethod method, string urlContains, HttpStatusCode statusCode, object? responseBody = null)
    {
        string? json = null;
        if (responseBody != null)
        {
            json = JsonSerializer.Serialize(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        _setups.Add((method, urlContains, statusCode, json));
    }

    public void SetupRaw(HttpMethod method, string urlContains, HttpStatusCode statusCode, string responseJson)
    {
        _setups.Add((method, urlContains, statusCode, responseJson));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content != null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : null;

        _requests.Add(new RecordedRequest(
            request.Method,
            request.RequestUri!.PathAndQuery,
            body,
            request.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray())));

        var url = request.RequestUri.PathAndQuery;

        foreach (var setup in _setups)
        {
            if (setup.Method == request.Method && url.Contains(setup.UrlContains))
            {
                var response = new HttpResponseMessage(setup.Status);
                if (setup.ResponseJson != null)
                {
                    response.Content = new StringContent(setup.ResponseJson, Encoding.UTF8, "application/json");
                }

                return response;
            }
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent($"No mock setup for {request.Method} {request.RequestUri}")
        };
    }
}

internal record RecordedRequest(
    HttpMethod Method,
    string Url,
    string? Body,
    Dictionary<string, string[]> Headers);
