using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class DatabaseStructureDto
{
    [JsonPropertyName("databaseName")]
    public string DatabaseName { get; set; } = default!;

    [JsonPropertyName("schemas")]
    public DatabaseSchemaDto[] Schemas { get; set; } = [];
}

public class DatabaseSchemaDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tables")]
    public DatabaseTableDto[] Tables { get; set; } = [];
}

public class DatabaseTableDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = default!;

    [JsonPropertyName("columns")]
    public DatabaseTableColumnDto[] Columns { get; set; } = [];
}

public class DatabaseTableColumnDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("clrType")]
    public string ClrType { get; set; } = default!;

    [JsonPropertyName("isPrimaryKey")]
    public bool IsPrimaryKey { get; set; }

    [JsonPropertyName("isForeignKey")]
    public bool IsForeignKey { get; set; }

    [JsonPropertyName("order")]
    public int? Order { get; set; }
}
