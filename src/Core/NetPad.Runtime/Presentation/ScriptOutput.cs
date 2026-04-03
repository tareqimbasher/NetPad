using System.Text.Json.Serialization;

namespace NetPad.Presentation;

public enum ScriptOutputKind
{
    Result,
    Sql,
    Error
}

public enum ScriptOutputFormat
{
    Text,
    Html,
    Json
}

[method: JsonConstructor]
public record ScriptOutput(
    ScriptOutputKind Kind,
    uint Order,
    string? Body,
    ScriptOutputFormat Format = ScriptOutputFormat.Text)
{
    public ScriptOutput(ScriptOutputKind kind, string? body, ScriptOutputFormat format = ScriptOutputFormat.Text)
        : this(kind, 0, body, format)
    {
    }
}
