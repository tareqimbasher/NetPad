using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class GetAllConnectionsResponse
{
    [JsonPropertyName("connections")]
    public DataConnectionDto[] Connections { get; set; } = [];

    [JsonPropertyName("servers")]
    public DataConnectionDto[] Servers { get; set; } = [];
}
