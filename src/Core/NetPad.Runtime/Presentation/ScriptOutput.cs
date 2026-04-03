using System.Text.Json.Serialization;

namespace NetPad.Presentation;

public enum ScriptOutputKind { Result, Sql, Error }
public enum ScriptOutputFormat { Text, Html, Json }

public record ScriptOutput
{
    public ScriptOutputKind Kind { get; init; }
    public uint Order { get; init; }
    public string? Body { get; init; }
    public ScriptOutputFormat Format { get; init; }

    [JsonConstructor]
    public ScriptOutput(ScriptOutputKind kind, uint order, string? body, ScriptOutputFormat format = ScriptOutputFormat.Text)
    {
        Kind = kind;
        Order = order;
        Body = body;
        Format = format;
    }

    public ScriptOutput(ScriptOutputKind kind, string? body, ScriptOutputFormat format = ScriptOutputFormat.Text)
        : this(kind, 0, body, format) { }
}
