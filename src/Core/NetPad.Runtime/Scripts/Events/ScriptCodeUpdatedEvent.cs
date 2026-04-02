using System.Text.Json.Serialization;

namespace NetPad.Scripts.Events;

public class ScriptCodeUpdatedEvent(Script script, string? newCode, string? oldCode, bool externallyInitiated = false)
    : IScriptEvent
{
    public Guid ScriptId { get; } = script.Id;

    [JsonIgnore]
    public Script Script { get; } = script;

    public string? NewCode { get; } = newCode;

    [JsonIgnore]
    public string? OldCode { get; } = oldCode;

    /// <summary>
    /// Indicates whether the code change was initiated by an external source (e.g. MCP, API)
    /// rather than the frontend editor. Used to determine whether to forward the event to IPC clients.
    /// </summary>
    public bool ExternallyInitiated { get; } = externallyInitiated;
}
