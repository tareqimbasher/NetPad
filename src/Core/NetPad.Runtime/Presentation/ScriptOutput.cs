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

/// <summary>
/// Output produced by running a user's script.
/// </summary>
/// <param name="Kind">The type of the output.</param>
/// <param name="Order">The order in which this output was emitted.</param>
/// <param name="Body">The body contents.</param>
/// <param name="Format">The format of the output.</param>
/// <param name="ElapsedMs">
/// Milliseconds between the start of user code and the moment this output was emitted.
/// </param>
[method: JsonConstructor]
public record ScriptOutput(
    ScriptOutputKind Kind,
    uint Order,
    string? Body,
    ScriptOutputFormat Format = ScriptOutputFormat.Text,
    long? ElapsedMs = null)
{
    public ScriptOutput(ScriptOutputKind kind, string? body, ScriptOutputFormat format = ScriptOutputFormat.Text)
        : this(kind, 0, body, format)
    {
    }
}
